namespace TestAgentMCP
{
	public partial class MainForm
	{
		private const int MaxFrames = 50;
		private const int MaxUndo = 10;
		private const int DefaultFps = 15;
		private const int PlayerHeight = 44;

		private sealed record ShapeData(int Id, Rectangle Bounds, Color Color, Shape Shape);

		private sealed record DocState(ShapeData[][] Frames, int Current, ShapeData[] Squares);

		// Animation: _frames contient une liste de formes par frame ; _squares est toujours la frame affichée
		// (donc c'est elle qu'on édite à la souris). Sans animation, _frames est vide.
		private List<ShapeData[]> _frames = [];
		private int _currentFrame;
		private int _fps = DefaultFps;

		private readonly List<DocState> _undo = [];
		private DocState? _dragUndo;
		private Rectangle _dragStart;

		private Panel? _pnlPlayer;
		private Button _btnPlay = null!;
		private TrackBar _slider = null!;
		private Label _lblFrame = null!;
		private System.Windows.Forms.Timer _playTimer = null!;

		private bool HasAnimation => _frames.Count > 1;

		private bool IsPlaying => _playTimer is { Enabled: true };

		private void InitializeAnimationUi()
		{
			var width = ClientSize.Width - pnlToolbar.Width;
			_pnlPlayer = new Panel
			{
				Visible = false,
				BackColor = Color.FromArgb(60, 60, 60),
				Size = new Size(width, PlayerHeight),
				Location = new Point(0, ClientSize.Height - PlayerHeight),
				Anchor = AnchorStyles.Bottom | AnchorStyles.Left
			};

			_btnPlay = new Button { Text = "▶", Location = new Point(8, 6), Size = new Size(44, 32), UseVisualStyleBackColor = true };
			_slider = new TrackBar
			{
				AutoSize = false,
				TickStyle = TickStyle.None,
				Minimum = 0,
				Maximum = 1,
				Location = new Point(60, 4),
				Size = new Size(width - 60 - 190, 36)
			};
			_lblFrame = new Label
			{
				ForeColor = Color.White,
				TextAlign = ContentAlignment.MiddleCenter,
				Location = new Point(width - 176, 6),
				Size = new Size(110, 32)
			};
			var btnClose = new Button { Text = "✕", Location = new Point(width - 60, 6), Size = new Size(48, 32), UseVisualStyleBackColor = true };

			_btnPlay.Click += (_, _) => TogglePlayback();
			_slider.Scroll += (_, _) => ShowFrame(_slider.Value);
			btnClose.Click += (_, _) => EndAnimation();
			_pnlPlayer.Controls.AddRange([_btnPlay, _slider, _lblFrame, btnClose]);
			Controls.Add(_pnlPlayer);
			_pnlPlayer.BringToFront();

			_playTimer = new System.Windows.Forms.Timer { Interval = 1000 / DefaultFps };
			_playTimer.Tick += (_, _) => ShowFrame((_currentFrame + 1) % _frames.Count);

			var mnuUndo = new ToolStripMenuItem("&Annuler") { ShortcutKeys = Keys.Control | Keys.Z };
			mnuUndo.Click += (_, _) => UndoOrTextUndo();
			var mnuEdit = new ToolStripMenuItem("&Édition");
			mnuEdit.DropDownItems.Add(mnuUndo);
			menuStrip.Items.Add(mnuEdit);
		}

		// ---- Snapshots / undo -------------------------------------------------------------------------

		private ShapeData[] Snapshot() =>
			_squares.Select(s => new ShapeData(s.Id, s.Bounds, s.Color, s.Shape)).ToArray();

		private void LoadShapes(ShapeData[] shapes)
		{
			_draggedSquare = null;
			_squares.Clear();
			_squares.AddRange(shapes.Select(d => new SquareState(d.Id, d.Bounds, d.Color, d.Shape)));
		}

		private void CommitCurrentFrame()
		{
			if (HasAnimation)
			{
				_frames[_currentFrame] = Snapshot();
			}
		}

		private DocState CaptureState()
		{
			CommitCurrentFrame();
			return new DocState(_frames.ToArray(), _currentFrame, Snapshot());
		}

		/// <summary>Mémorise l'état avant une modification (10 états max). À appeler AVANT de modifier.</summary>
		private void PushUndo(DocState? state = null)
		{
			_undo.Add(state ?? CaptureState());
			if (_undo.Count > MaxUndo)
			{
				_undo.RemoveAt(0);
			}
		}

		private void UndoOrTextUndo()
		{
			// Dans la zone de saisie, Ctrl+Z reste l'annulation de texte.
			if (ActiveControl is TextBox { ReadOnly: false, CanUndo: true } textBox)
			{
				textBox.Undo();
				return;
			}

			Undo();
		}

		private void Undo()
		{
			if (_undo.Count == 0)
			{
				AppendStatus("Rien à annuler.");
				return;
			}

			StopPlayback();
			var state = _undo[^1];
			_undo.RemoveAt(_undo.Count - 1);
			_frames = [.. state.Frames];
			_currentFrame = state.Current;
			LoadShapes(state.Squares);
			RefreshPlayer();
			Invalidate();
			AppendStatus($"Annulation effectuée ({_undo.Count} état(s) restant(s)).");
			LogLine("UNDO", $"Remaining={_undo.Count}");
		}

		private int NextShapeId() =>
			Math.Max(
				_squares.Select(s => s.Id).DefaultIfEmpty(0).Max(),
				_frames.SelectMany(f => f).Select(s => s.Id).DefaultIfEmpty(0).Max()) + 1;

		// ---- Animation ---------------------------------------------------------------------------------

		/// <summary>
		/// Remplace l'animation (2 à 50 frames). Chaque frame est appliquée en "Update" sur la précédente ;
		/// la première part de l'état courant. 0 frame supprime l'animation, 1 frame est une simple mise à jour.
		/// </summary>
		private void ApplyAnimation(LocalMcpServer.AnimationRequest request)
		{
			var frames = request.Frames ?? [];
			if (frames.Count > MaxFrames)
			{
				throw new ArgumentException($"Animation limitée à {MaxFrames} frames ({frames.Count} reçues).");
			}

			StopPlayback();
			PushUndo();

			// La frame affichée devient la base ; l'animation précédente est écrasée.
			_frames = [];
			_currentFrame = 0;

			if (frames.Count == 0)
			{
				RefreshPlayer();
				Invalidate();
				AppendStatus("Animation supprimée.");
				return;
			}

			var built = new List<ShapeData[]>();
			foreach (var infos in frames)
			{
				ApplyAgentResult(new AgentMCP.AgentResult { Action = "Update", Infos = infos });
				built.Add(Snapshot());
			}

			if (built.Count == 1)
			{
				RefreshPlayer();
				Invalidate();
				return;
			}

			_fps = Math.Clamp(request.Fps ?? DefaultFps, 1, 60);
			_frames = built;
			LoadShapes(built[0]);
			RefreshPlayer();
			StartPlayback();
			Invalidate();
			AppendStatus($"Animation chargée: {built.Count} frame(s) à {_fps} fps.");
			LogLine("ANIMATION", $"Frames={built.Count};Fps={_fps}");
		}

		private void EndAnimation()
		{
			if (!HasAnimation)
			{
				return;
			}

			StopPlayback();
			PushUndo();
			_frames = [];
			_currentFrame = 0;
			RefreshPlayer();
			Invalidate();
			AppendStatus("Animation terminée: la frame affichée devient l'état courant.");
		}

		private void ShowFrame(int index)
		{
			if (!HasAnimation)
			{
				return;
			}

			CommitCurrentFrame();
			_currentFrame = Math.Clamp(index, 0, _frames.Count - 1);
			LoadShapes(_frames[_currentFrame]);
			SyncPlayerUi();
			Invalidate();
		}

		private void TogglePlayback()
		{
			if (IsPlaying)
			{
				StopPlayback();
			}
			else
			{
				StartPlayback();
			}
		}

		private void StartPlayback()
		{
			if (!HasAnimation)
			{
				return;
			}

			CommitCurrentFrame();
			_playTimer.Interval = Math.Max(1, 1000 / _fps);
			_playTimer.Start();
			SyncPlayerUi();
		}

		private void StopPlayback()
		{
			_playTimer?.Stop();
			SyncPlayerUi();
		}

		/// <summary>Affiche/masque le lecteur ; la fenêtre grandit pour que la zone de dessin ne change pas.</summary>
		private void RefreshPlayer()
		{
			if (_pnlPlayer is null)
			{
				return;
			}

			var show = HasAnimation;
			if (show != _pnlPlayer.Visible)
			{
				if (!show)
				{
					_pnlPlayer.Visible = false;
				}

				ClientSize = new Size(ClientSize.Width, ClientSize.Height + (show ? PlayerHeight : -PlayerHeight));
				_pnlPlayer.Location = new Point(0, ClientSize.Height - PlayerHeight);
				_pnlPlayer.Visible = show;
			}

			SyncPlayerUi();
		}

		private void SyncPlayerUi()
		{
			if (_pnlPlayer is null || !HasAnimation)
			{
				return;
			}

			_slider.Maximum = _frames.Count - 1;
			_slider.Value = Math.Clamp(_currentFrame, 0, _slider.Maximum);
			_lblFrame.Text = $"{_currentFrame + 1} / {_frames.Count}";
			_btnPlay.Text = IsPlaying ? "⏸" : "▶";
		}
	}
}

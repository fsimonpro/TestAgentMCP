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

		private bool HasAnimation => _frames.Count > 1;

		private bool IsPlaying => playTimer.Enabled;

		// Les contrôles du lecteur et le menu Édition sont déclarés dans MainForm.Designer.cs ; ici seulement les événements.
		private void WireAnimationEvents()
		{
			btnPlay.Click += (_, _) => TogglePlayback();
			sliderFrame.Scroll += (_, _) => ShowFrame(sliderFrame.Value);
			btnAddFrame.Click += (_, _) => AddFrame();
			btnDeleteFrame.Click += (_, _) => DeleteCurrentFrame();
			playTimer.Tick += (_, _) => ShowFrame((_currentFrame + 1) % _frames.Count);
			mnuUndo.Click += (_, _) => UndoOrTextUndo();
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
		private void ApplyAnimation(AnimationRequest request)
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

		/// <summary>Insère une copie de la frame affichée juste après elle et s'y positionne.</summary>
		private void AddFrame()
		{
			if (_frames.Count >= MaxFrames)
			{
				AppendStatus($"Animation limitée à {MaxFrames} frames.");
				return;
			}

			StopPlayback();
			PushUndo();
			if (!HasAnimation)
			{
				// Sans animation : la scène devient la frame 1 et on passe sur une copie (frame 2).
				_frames = [Snapshot()];
				_currentFrame = 0;
			}

			_frames.Insert(_currentFrame + 1, Snapshot());
			_currentFrame++;
			SyncPlayerUi();
			Invalidate();
			AppendStatus($"Frame ajoutée ({_currentFrame + 1} / {_frames.Count}).");
		}

		/// <summary>Supprime la frame affichée ; s'il n'en reste qu'une, l'animation se termine.</summary>
		private void DeleteCurrentFrame()
		{
			if (!HasAnimation)
			{
				return;
			}

			StopPlayback();
			PushUndo();
			_frames.RemoveAt(_currentFrame);
			_currentFrame = Math.Min(_currentFrame, _frames.Count - 1);
			LoadShapes(_frames[_currentFrame]);
			if (_frames.Count == 1)
			{
				_frames = [];
				_currentFrame = 0;
				RefreshPlayer();
				AppendStatus("Frame supprimée: il ne reste qu'une frame, l'animation est terminée.");
			}
			else
			{
				SyncPlayerUi();
				AppendStatus($"Frame supprimée ({_currentFrame + 1} / {_frames.Count}).");
			}

			Invalidate();
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
			playTimer.Interval = Math.Max(1, 1000 / _fps);
			playTimer.Start();
			SyncPlayerUi();
		}

		private void StopPlayback()
		{
			playTimer.Stop();
			SyncPlayerUi();
		}

		/// <summary>La timeline est toujours visible (« + » crée l'animation) ; la fenêtre grandit une fois pour qu'elle ne réduise pas la zone de dessin.</summary>
		private void RefreshPlayer()
		{
			if (!pnlPlayer.Visible)
			{
				ClientSize = new Size(ClientSize.Width, ClientSize.Height + PlayerHeight);
				pnlPlayer.Location = new Point(0, ClientSize.Height - PlayerHeight);
				pnlPlayer.Visible = true;
			}

			SyncPlayerUi();
		}

		private void SyncPlayerUi()
		{
			var animated = HasAnimation;
			btnPlay.Enabled = animated;
			sliderFrame.Enabled = animated;
			btnDeleteFrame.Enabled = animated;

			if (!animated)
			{
				sliderFrame.Value = 0;
				lblFrame.Text = "1 / 1";
				btnPlay.Text = "▶";
				return;
			}

			sliderFrame.Maximum = _frames.Count - 1;
			sliderFrame.Value = Math.Clamp(_currentFrame, 0, sliderFrame.Maximum);
			lblFrame.Text = $"{_currentFrame + 1} / {_frames.Count}";
			btnPlay.Text = IsPlaying ? "⏸" : "▶";
		}
	}
}

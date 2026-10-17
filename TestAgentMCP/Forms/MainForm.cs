using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace TestAgentMCP
{
	public partial class MainForm : Form
	{
		private const int DefaultSquareSize = 60;
		private readonly Random _random = new();
		private readonly AgentMCP _agent;
		private readonly List<SquareState> _squares = new();
		private readonly string _logFilePath;
		private readonly object _logLock = new();
        private readonly LocalMcpServer _mcpServer;
		private SquareState? _draggedSquare;
		private Point _dragOffset;
		private Color _selectedColor = Color.Blue;

		public MainForm()
		{
			InitializeComponent();
			DoubleBuffered = true;
            _mcpServer = new LocalMcpServer(BuildCanvasState, ApplyAgentResultFromMcp);
			_agent = new AgentMCP(
               ProjectSettings.OpenAiApiKey,
              new LocalMcpClient(new InMemoryMcpTransport(_mcpServer)),
				message => LogLine("AGENT_PROMPT", message));
			_logFilePath = Path.Combine(AppContext.BaseDirectory, "output.log");
			LogLine("APP", "Application démarrée.");
			InitializeSquares();
			btnApplyPrompt.Click += btnApplyPrompt_Click;
			btnOpenLog.Click += btnOpenLog_Click;
           btnAddSquare.Click += (_, _) => AddShape(Shape.Square);
			btnAddCircle.Click += (_, _) => AddShape(Shape.Circle);
			btnAddLosange.Click += (_, _) => AddShape(Shape.Losange);
           btnClearAll.Click += btnClearAll_Click;
			btnColor.Click += btnColor_Click;
			btnColor.Paint += btnColor_Paint;
			mnuSavePreset.Click += (_, _) => SavePreset();
			mnuLoadPreset.Click += (_, _) => LoadPreset();
            chkDeleteMode.CheckedChanged += chkDeleteMode_CheckedChanged;
            MouseDown += MainForm_MouseDown;
			MouseMove += MainForm_MouseMove;
			MouseUp += MainForm_MouseUp;
			Shown += (_, _) => StartHttpMcpServer();
			FormClosed += (_, _) => _httpMcpHost?.Dispose();
			SetStatus(_agent.IsConfigured
				? "Agent prêt. Saisis un prompt puis clique sur Envoyer."
              : "Clé manquante: configure OpenAiApiKey dans le fichier projet (.csproj).");
		}

		// Les appels MCP rapprochés (animation) sont regroupés en une seule ligne de log.
		private readonly Dictionary<string, int> _mcpPending = new();
		private System.Windows.Forms.Timer? _mcpSummaryTimer;

		private void RecordMcpCall(string description)
		{
			_mcpPending[description] = _mcpPending.GetValueOrDefault(description) + 1;
			if (_mcpSummaryTimer == null)
			{
				_mcpSummaryTimer = new System.Windows.Forms.Timer { Interval = 700 };
				_mcpSummaryTimer.Tick += (_, _) =>
				{
					_mcpSummaryTimer.Stop();
					AppendStatus("MCP: " + string.Join(", ", _mcpPending.Select(p => $"{p.Value}× {p.Key}")));
					_mcpPending.Clear();
				};
			}

			_mcpSummaryTimer.Stop();
			_mcpSummaryTimer.Start();
		}

		private const int HttpMcpPort = 3333;
		private HttpMcpHost? _httpMcpHost;

		private void StartHttpMcpServer()
		{
			// Les requêtes HTTP arrivent sur des threads du pool : on les marshale sur le thread UI.
			var httpServer = new LocalMcpServer(
				() => (LocalMcpServer.CanvasState)Invoke(() =>
				{
					RecordMcpCall("get_canvas_state");
					return BuildCanvasState();
				}),
				result => Invoke(() =>
				{
					RecordMcpCall($"{result.Action} de {result.Infos.Count} forme(s)");
					ApplyAgentResultFromMcp(result);
				}));
			try
			{
				_httpMcpHost = new HttpMcpHost(httpServer, HttpMcpPort, message => LogLine("MCP_HTTP", message));
				_httpMcpHost.Start();
				AppendStatus($"Serveur MCP à l'écoute: {_httpMcpHost.Url}");
			}
			catch (Exception ex)
			{
				_httpMcpHost = null;
				AppendStatus($"Serveur MCP HTTP non démarré: {ex.Message}");
				LogLine("ERROR", ex.ToString());
			}
		}

        private void AddShape(Shape shape)
		{
			var newId = _squares.Count == 0 ? 1 : _squares.Max(s => s.Id) + 1;
            var rect = GetRandomRectangle(_squares.Select(s => s.Bounds).ToList());
			var color = _selectedColor;

          _squares.Add(new SquareState(newId, rect, color, shape));
			AppendStatus($"Forme {shape} #{newId} ajoutée en position aléatoire ({rect.X},{rect.Y}) avec couleur {ColorTranslator.ToHtml(color)}.");
			LogLine("ADD", $"SquareId={newId};Shape={shape};X={rect.X};Y={rect.Y};Size={rect.Width};Color={ColorTranslator.ToHtml(color)}");
			Invalidate();
		}

		private void MainForm_MouseDown(object? sender, MouseEventArgs e)
		{
            var selectedSquare = _squares.LastOrDefault(square => IsPointInsideShape(square, e.Location));

			if (e.Button == MouseButtons.Right)
			{
				if (selectedSquare is null)
				{
					return;
				}

				using var colorDialog = new ColorDialog
				{
					Color = selectedSquare.Color,
					FullOpen = true
				};

				if (colorDialog.ShowDialog(this) == DialogResult.OK)
				{
					selectedSquare.Color = colorDialog.Color;
					AppendStatus($"Couleur du carré {selectedSquare.Id} modifiée en {ColorTranslator.ToHtml(selectedSquare.Color)}.");
					LogLine("COLOR", $"SquareId={selectedSquare.Id};Color={ColorTranslator.ToHtml(selectedSquare.Color)}");
					Invalidate();
				}

				return;
			}

			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			if (selectedSquare is null)
			{
				return;
			}

			if (chkDeleteMode.Checked)
			{
				_squares.Remove(selectedSquare);
				AppendStatus($"Carré {selectedSquare.Id} supprimé (mode delete). ");
				LogLine("DELETE", $"SquareId={selectedSquare.Id}");
				Invalidate();
				return;
			}

			_draggedSquare = selectedSquare;

			_dragOffset = new Point(e.X - _draggedSquare.Bounds.X, e.Y - _draggedSquare.Bounds.Y);
		}

		private void chkDeleteMode_CheckedChanged(object? sender, EventArgs e)
		{
			if (chkDeleteMode.Checked)
			{
				_draggedSquare = null;
				AppendStatus("Mode suppression activé: clic sur un carré pour le supprimer.");
				LogLine("MODE", "DeleteMode=On");
             Invalidate();
				return;
			}

			AppendStatus("Mode suppression désactivé.");
			LogLine("MODE", "DeleteMode=Off");
           Invalidate();
		}

		private void btnColor_Paint(object? sender, PaintEventArgs e)
		{
			var g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			var h = btnColor.Height;

			// Pipette: embout en bas à gauche, bulbe en haut à droite
			using (var bulb = new Pen(Color.DimGray, 6) { StartCap = LineCap.Round, EndCap = LineCap.Round })
			using (var tube = new Pen(Color.Black, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
			{
				g.DrawLine(tube, 6, h - 8, 17, 14);
				g.DrawLine(bulb, 17, 14, 22, 8);
			}

			// Carré de la couleur en cours
			var swatch = new Rectangle(btnColor.Width - 24, (h - 18) / 2, 18, 18);
			using var fill = new SolidBrush(_selectedColor);
			g.FillRectangle(fill, swatch);

			g.DrawRectangle(Pens.Black, swatch);
		}

		private void btnColor_Click(object? sender, EventArgs e)
		{
			using var colorDialog = new ColorDialog
			{
				Color = _selectedColor,
				FullOpen = true
			};

			if (colorDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			_selectedColor = colorDialog.Color;
			btnColor.Invalidate();
			AppendStatus($"Couleur des prochaines formes: {ColorTranslator.ToHtml(colorDialog.Color)}.");
			LogLine("COLOR_SELECT", $"Color={ColorTranslator.ToHtml(colorDialog.Color)}");
		}

		private void btnClearAll_Click(object? sender, EventArgs e)
		{
			if (_squares.Count == 0)
			{
				AppendStatus("Aucune forme à supprimer.");
				return;
			}

			var removedCount = _squares.Count;
			_squares.Clear();
			_draggedSquare = null;
			AppendStatus($"Suppression totale: {removedCount} forme(s) supprimée(s).");
			LogLine("CLEAR", $"RemovedCount={removedCount}");
			Invalidate();
		}

		private void MainForm_MouseMove(object? sender, MouseEventArgs e)
		{
			if (_draggedSquare is null || e.Button != MouseButtons.Left)
			{
				return;
			}

			var area = GetDrawingArea();
			var maxX = Math.Max(area.Left, area.Right - _draggedSquare.Bounds.Width);
			var maxY = Math.Max(area.Top, area.Bottom - _draggedSquare.Bounds.Height);

			var x = Math.Clamp(e.X - _dragOffset.X, area.Left, maxX);
			var y = Math.Clamp(e.Y - _dragOffset.Y, area.Top, maxY);
			_draggedSquare.Bounds = new Rectangle(x, y, _draggedSquare.Bounds.Width, _draggedSquare.Bounds.Height);
			Invalidate();
		}

		private void MainForm_MouseUp(object? sender, MouseEventArgs e)
		{
			if (_draggedSquare is null)
			{
				return;
			}

			AppendStatus($"Carré {_draggedSquare.Id} déplacé à X={_draggedSquare.Bounds.X}, Y={_draggedSquare.Bounds.Y}.");
			_logDrag(_draggedSquare);
			_draggedSquare = null;
		}

		private void btnOpenLog_Click(object? sender, EventArgs e)
		{
			try
			{
				if (!File.Exists(_logFilePath))
				{
					File.WriteAllText(_logFilePath, string.Empty);
				}

				Process.Start(new ProcessStartInfo
				{
					FileName = _logFilePath,
					UseShellExecute = true
				});

				AppendStatus("Ouverture du fichier output.log...");
			}
			catch (Exception ex)
			{
				AppendStatus($"Impossible d'ouvrir le log: {ex.Message}");
				LogLine("ERROR", ex.ToString());
			}
		}

		private Color GetRandomColor()
		{
			return Color.FromArgb(
				255,
				_random.Next(40, 256),
				_random.Next(40, 256),
				_random.Next(40, 256));
		}

		private void _logDrag(SquareState square)
		{
			LogLine("DRAG", $"SquareId={square.Id};X={square.Bounds.X};Y={square.Bounds.Y}");
		}

		private async void btnApplyPrompt_Click(object? sender, EventArgs e)
		{
			if (!_agent.IsConfigured)
			{
              SetStatus("Clé API absente. Ajoute OpenAiApiKey dans le .csproj puis redémarre l'application.");
				return;
			}

			var prompt = txtPrompt.Text.Trim();
			if (string.IsNullOrWhiteSpace(prompt))
			{
				SetStatus("Saisis un prompt.");
				return;
			}

			btnApplyPrompt.Enabled = false;
			SetStatus($"Prompt reçu: \"{prompt}\".");
			LogLine("PROMPT", prompt);
			AppendStatus("Collecte de l'état des carrés (positions/couleurs)...");

			try
			{
                AppendStatus($"Etat courant envoyé à l'agent via MCP ({_squares.Count} carré(s)).");
				AppendStatus("Appel du modèle OpenAI en cours...");

                var result = await _agent.GetActionAsync(prompt);
				LogLine("PROMPT_RESULT", System.Text.Json.JsonSerializer.Serialize(result));
				AppendStatus($"Réponse reçue. Action: {result.Action ?? "(vide)"}. Mises à jour: {result.Infos.Count}.");
              AppendStatus("Mise à jour appliquée par MCP.");
				AppendStatus(result.Message ?? "Action appliquée.");
				AppendStatus("Rendu terminé.");
				txtPrompt.Text = string.Empty;
			}
			catch (Exception ex)
			{
				AppendStatus($"Erreur agent: {ex.Message}");
				LogLine("ERROR", ex.ToString());
			}
			finally
			{
				btnApplyPrompt.Enabled = true;
			}
		}

		private LocalMcpServer.CanvasState BuildCanvasState()
		{
			var area = GetDrawingArea();
			return new LocalMcpServer.CanvasState
			{
				ClientWidth = ClientSize.Width,
				ClientHeight = ClientSize.Height,
				DrawingLeft = area.Left,
				DrawingTop = area.Top,
				DrawingWidth = area.Width,
				DrawingHeight = area.Height,
				Squares = _squares
					.Select(square => new LocalMcpServer.CanvasSquare
					{
						Id = square.Id,
						X = square.Bounds.X,
						Y = square.Bounds.Y,
						Size = square.Bounds.Width,
                        Color = ColorTranslator.ToHtml(square.Color),
						Shape = square.Shape.ToString()
					})
					.ToList()
			};
		}

		private void ApplyAgentResultFromMcp(AgentMCP.AgentResult result)
		{
			ApplyAgentResult(result);
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var drawingArea = GetDrawingArea();
			e.Graphics.SetClip(drawingArea);
			using var deleteModePen = new Pen(Color.White, 2)
			{
				DashStyle = DashStyle.Dash
			};

			foreach (var square in _squares)
			{
				using var brush = new SolidBrush(square.Color);
             DrawShape(e.Graphics, brush, square);

				if (chkDeleteMode.Checked)
				{
                  DrawShapeOutline(e.Graphics, deleteModePen, square);
				}
			}

			e.Graphics.ResetClip();
		}

		private void InitializeSquares()
		{
			var area = GetDrawingArea();
			var baseY = area.Top + 20;

            _squares.Add(new SquareState(1, new Rectangle(40, baseY, DefaultSquareSize, DefaultSquareSize), Color.Red, Shape.Square));
			_squares.Add(new SquareState(2, new Rectangle(140, baseY + 80, DefaultSquareSize, DefaultSquareSize), Color.Green, Shape.Circle));
			_squares.Add(new SquareState(3, new Rectangle(260, baseY + 40, DefaultSquareSize, DefaultSquareSize), Color.Blue, Shape.Losange));

		}

		private Rectangle GetRandomRectangle(IReadOnlyList<Rectangle> existingSquares)
		{
			var area = GetDrawingArea();
			int maxX = Math.Max(area.Left, area.Right - DefaultSquareSize);
			int maxY = Math.Max(area.Top, area.Bottom - DefaultSquareSize);

			for (int i = 0; i < 50; i++)
			{
				var rect = new Rectangle(
				 _random.Next(area.Left, maxX + 1),
					_random.Next(area.Top, maxY + 1),
					DefaultSquareSize,
					DefaultSquareSize);

				if (existingSquares.All(existing => !existing.IntersectsWith(rect)))
				{
					return rect;
				}
			}

			return new Rectangle(
			 _random.Next(area.Left, maxX + 1),
				_random.Next(area.Top, maxY + 1),
				DefaultSquareSize,
				DefaultSquareSize);
		}

		private Rectangle GetDrawingArea()
		{
			var top = txtStatus.Bottom + 12;
			var height = Math.Max(1, ClientSize.Height - top - 12);
         var rightLimit = pnlToolbar.Left - 6;
			var width = Math.Max(1, rightLimit);
			return new Rectangle(0, top, width, height);
		}

		private void SetStatus(string message)
		{
			txtStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
			LogLine("STATUS", message);
		}

		private void AppendStatus(string message)
		{
			if (string.IsNullOrWhiteSpace(txtStatus.Text))
			{
				SetStatus(message);
				return;
			}

			txtStatus.AppendText(Environment.NewLine + $"[{DateTime.Now:HH:mm:ss}] {message}");
			LogLine("STATUS", message);
		}

		private void LogLine(string category, string message)
		{
			try
			{
				var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}{Environment.NewLine}";
				lock (_logLock)
				{
					File.AppendAllText(_logFilePath, line);
				}
			}
			catch
			{
			}
		}

      private void ApplyAgentResult(AgentMCP.AgentResult result)
		{
			if (result.Action != null && result.Action.Equals("Remove", StringComparison.InvariantCultureIgnoreCase))
			{
				foreach (var info in result.Infos)
				{
					var square = _squares.FirstOrDefault(s => s.Id == info.Id);
					if (square != null)
					{
						_squares.Remove(square);
					}
				}
				return;
			}

			if (result.Action != null && result.Action.Equals("Add", StringComparison.InvariantCultureIgnoreCase))
			{
					var nextId = Math.Max(result.Infos.Select(s => s.Id).DefaultIfEmpty(0).Max(), _squares.Select(s => s.Id).DefaultIfEmpty(0).Max()) + 1;
					foreach (var info in result.Infos)
					{
						if (info.Id <= 0 || _squares.Any(s => s.Id == info.Id))
						{
							info.Id = nextId++;
						}
					}
			}

			foreach (var info in result.Infos)
			{
				var square = _squares.FirstOrDefault(s => s.Id == info.Id);
				if (square == null)
				{
					txtStatus.AppendText(Environment.NewLine + $"Carré avec Id {info.Id} non trouvé, création d'un nouveau carré.");
                 square = new SquareState(info.Id, GetRandomRectangle(_squares.Select(s => s.Bounds).ToList()), Color.Gray, Shape.Square);
					_squares.Add(square);
				}

				if (info.Color is not null && TryParseColor(info.Color, out var parsedColor))
				{
					square.Color = parsedColor;
				}

				if (info.Shape is not null && TryParseShape(info.Shape, out var parsedShape))
				{
					square.Shape = parsedShape;
				}

				var area = GetDrawingArea();
				var newSize = info.Size ?? square.Bounds.Width;
				var maxX = Math.Max(area.Left, area.Right - newSize);
				var maxY = Math.Max(area.Top, area.Bottom - newSize);

				var x = Math.Clamp(info.X ?? square.Bounds.X, area.Left, maxX);
				var y = Math.Clamp(info.Y ?? square.Bounds.Y, area.Top, maxY);

				square.Bounds = new Rectangle(x, y, newSize, newSize);
			}
		}

		private static bool TryParseColor(string colorValue, out Color color)
		{
			try
			{
				color = ColorTranslator.FromHtml(colorValue);
				return true;
			}
			catch
			{
				color = default;
				return false;
			}
		}

		private static bool TryParseShape(string? shapeValue, out Shape shape)
		{
			if (string.IsNullOrWhiteSpace(shapeValue))
			{
				shape = Shape.Square;
				return false;
			}

			shape = shapeValue.Trim().ToLowerInvariant() switch
			{
				"square" or "carre" or "carré" => Shape.Square,
				"circle" or "cercle" => Shape.Circle,
				"losange" or "diamond" => Shape.Losange,
				_ => Shape.Square
			};

			return true;
		}

		private static void DrawShape(Graphics graphics, Brush brush, SquareState square)
		{
			switch (square.Shape)
			{
				case Shape.Circle:
					graphics.FillEllipse(brush, square.Bounds);
					break;
				case Shape.Losange:
					graphics.FillPolygon(brush, GetLosangePoints(square.Bounds));
					break;
				default:
					graphics.FillRectangle(brush, square.Bounds);
					break;
			}
		}

		private static void DrawShapeOutline(Graphics graphics, Pen pen, SquareState square)
		{
			var borderRect = Rectangle.Inflate(square.Bounds, -1, -1);
			switch (square.Shape)
			{
				case Shape.Circle:
					graphics.DrawEllipse(pen, borderRect);
					break;
				case Shape.Losange:
					graphics.DrawPolygon(pen, GetLosangePoints(borderRect));
					break;
				default:
					graphics.DrawRectangle(pen, borderRect);
					break;
			}
		}

		private static Point[] GetLosangePoints(Rectangle bounds)
		{
			return
			[
				new Point(bounds.Left + (bounds.Width / 2), bounds.Top),
				new Point(bounds.Right, bounds.Top + (bounds.Height / 2)),
				new Point(bounds.Left + (bounds.Width / 2), bounds.Bottom),
				new Point(bounds.Left, bounds.Top + (bounds.Height / 2))
			];
		}

		private static bool IsPointInsideShape(SquareState square, Point point)
		{
			switch (square.Shape)
			{
				case Shape.Circle:
					var centerX = square.Bounds.Left + (square.Bounds.Width / 2d);
					var centerY = square.Bounds.Top + (square.Bounds.Height / 2d);
					var radius = square.Bounds.Width / 2d;
					var dx = point.X - centerX;
					var dy = point.Y - centerY;
					return (dx * dx) + (dy * dy) <= radius * radius;
				case Shape.Losange:
					var localX = Math.Abs(point.X - (square.Bounds.Left + square.Bounds.Width / 2d));
					var localY = Math.Abs(point.Y - (square.Bounds.Top + square.Bounds.Height / 2d));
					return (localX / (square.Bounds.Width / 2d)) + (localY / (square.Bounds.Height / 2d)) <= 1;
				default:
					return square.Bounds.Contains(point);
			}
		}

     private sealed class SquareState(int Id, Rectangle bounds, Color color, Shape shape)
		{
			public int Id { get; } = Id;
			public Rectangle Bounds { get; set; } = bounds;
			public Color Color { get; set; } = color;
           public Shape Shape { get; set; } = shape;
		}

	}
}

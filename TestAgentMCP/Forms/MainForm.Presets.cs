using System.Text.Json;

namespace TestAgentMCP
{
	public partial class MainForm
	{
		private const string PresetFilter = "Preset de formes (*.json)|*.json";
		private static readonly JsonSerializerOptions PresetJsonOptions = new() { WriteIndented = true };

		private sealed record PresetShape(int Id, int X, int Y, int Size, string Color, string Shape);

		// Un preset est soit un tableau de formes (état fixe, ancien format), soit cet objet (animation).
		private sealed record PresetAnimation(int Fps, List<List<PresetShape>> Frames);

		private static string PresetsFolder => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TestAgentMCP", "Presets");

		// Coordonnées relatives à la zone de dessin : le preset reste valable si la mise en page change.
		private PresetShape ToPreset(ShapeData s, Rectangle area) =>
			new(s.Id, s.Bounds.X - area.Left, s.Bounds.Y - area.Top, s.Bounds.Width, ColorTranslator.ToHtml(s.Color), s.Shape.ToString());

		private static ShapeData FromPreset(PresetShape shape, Rectangle area)
		{
			TryParseColor(shape.Color, out var color);
			TryParseShape(shape.Shape, out var kind);
			var size = Math.Max(1, shape.Size);
			var x = Math.Clamp(area.Left + shape.X, area.Left, Math.Max(area.Left, area.Right - size));
			var y = Math.Clamp(area.Top + shape.Y, area.Top, Math.Max(area.Top, area.Bottom - size));
			return new ShapeData(shape.Id, new Rectangle(x, y, size, size), color.IsEmpty ? Color.Gray : color, kind);
		}

		private void SavePreset()
		{
			Directory.CreateDirectory(PresetsFolder);
			using var dialog = new SaveFileDialog
			{
				Title = "Enregistrer le preset",
				Filter = PresetFilter,
				DefaultExt = "json",
				InitialDirectory = PresetsFolder
			};

			if (dialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			try
			{
				var area = GetDrawingArea();
				CommitCurrentFrame();

				object payload;
				string description;
				if (HasAnimation)
				{
					payload = new PresetAnimation(_fps, _frames.Select(f => f.Select(s => ToPreset(s, area)).ToList()).ToList());
					description = $"animation de {_frames.Count} frame(s) à {_fps} fps";
				}
				else
				{
					var shapes = Snapshot().Select(s => ToPreset(s, area)).ToList();
					payload = shapes;
					description = $"{shapes.Count} forme(s)";
				}

				File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(payload, PresetJsonOptions));
				AppendStatus($"Preset enregistré: {dialog.FileName} ({description}).");
				LogLine("PRESET_SAVE", $"File={dialog.FileName};{description}");
			}
			catch (Exception ex)
			{
				AppendStatus($"Impossible d'enregistrer le preset: {ex.Message}");
				LogLine("ERROR", ex.ToString());
			}
		}

		private void LoadPreset()
		{
			Directory.CreateDirectory(PresetsFolder);
			using var dialog = new OpenFileDialog
			{
				Title = "Charger un preset",
				Filter = PresetFilter,
				InitialDirectory = PresetsFolder
			};

			if (dialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			try
			{
				using var document = JsonDocument.Parse(File.ReadAllText(dialog.FileName));
				var area = GetDrawingArea();

				if (document.RootElement.ValueKind == JsonValueKind.Object)
				{
					LoadAnimationPreset(document.RootElement.Deserialize<PresetAnimation>()
						?? throw new InvalidDataException("Fichier vide."), area, dialog.FileName);
					return;
				}

				var shapes = document.RootElement.Deserialize<List<PresetShape>>()
					?? throw new InvalidDataException("Fichier vide.");

				// Preset fixe : remplace la frame affichée (donc l'état entier s'il n'y a pas d'animation).
				var loaded = shapes.Select(s => FromPreset(s, area)).ToArray();
				PushUndo();
				LoadShapes(loaded);
				AppendStatus($"Preset chargé: {dialog.FileName} ({loaded.Length} forme(s)).");
				LogLine("PRESET_LOAD", $"File={dialog.FileName};Count={loaded.Length}");
				Invalidate();
			}
			catch (Exception ex)
			{
				AppendStatus($"Impossible de charger le preset: {ex.Message}");
				LogLine("ERROR", ex.ToString());
			}
		}

		private void LoadAnimationPreset(PresetAnimation preset, Rectangle area, string fileName)
		{
			if (preset.Frames is null || preset.Frames.Count == 0)
			{
				throw new InvalidDataException("Animation sans frame.");
			}

			if (preset.Frames.Count > MaxFrames)
			{
				throw new InvalidDataException($"Animation limitée à {MaxFrames} frames ({preset.Frames.Count} dans le fichier).");
			}

			var frames = preset.Frames.Select(f => f.Select(s => FromPreset(s, area)).ToArray()).ToList();
			StopPlayback();
			PushUndo();

			if (frames.Count == 1)
			{
				_frames = [];
				_currentFrame = 0;
				LoadShapes(frames[0]);
				RefreshPlayer();
			}
			else
			{
				_fps = Math.Clamp(preset.Fps, 1, 60);
				_frames = frames;
				_currentFrame = 0;
				LoadShapes(frames[0]);
				RefreshPlayer();
				StartPlayback();
			}

			AppendStatus($"Preset chargé: {fileName} ({frames.Count} frame(s) à {_fps} fps).");
			LogLine("PRESET_LOAD", $"File={fileName};Frames={frames.Count};Fps={_fps}");
			Invalidate();
		}
	}
}

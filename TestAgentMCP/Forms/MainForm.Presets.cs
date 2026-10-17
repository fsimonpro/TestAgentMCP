using System.Text.Json;

namespace TestAgentMCP
{
	public partial class MainForm
	{
		private const string PresetFilter = "Preset de formes (*.json)|*.json";
		private static readonly JsonSerializerOptions PresetJsonOptions = new() { WriteIndented = true };

		private sealed record PresetShape(int Id, int X, int Y, int Size, string Color, string Shape);

		private static string PresetsFolder => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TestAgentMCP", "Presets");

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
				// Coordonnées relatives à la zone de dessin : le preset reste valable si la mise en page change.
				var area = GetDrawingArea();
				var shapes = _squares
					.Select(s => new PresetShape(s.Id, s.Bounds.X - area.Left, s.Bounds.Y - area.Top, s.Bounds.Width, ColorTranslator.ToHtml(s.Color), s.Shape.ToString()))
					.ToList();

				File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(shapes, PresetJsonOptions));
				AppendStatus($"Preset enregistré: {dialog.FileName} ({shapes.Count} forme(s)).");
				LogLine("PRESET_SAVE", $"File={dialog.FileName};Count={shapes.Count}");
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
				var shapes = JsonSerializer.Deserialize<List<PresetShape>>(File.ReadAllText(dialog.FileName))
					?? throw new InvalidDataException("Fichier vide.");

				var area = GetDrawingArea();
				var loaded = new List<SquareState>();
				foreach (var shape in shapes)
				{
					TryParseColor(shape.Color, out var color);
					TryParseShape(shape.Shape, out var kind);
					var size = Math.Max(1, shape.Size);
					var x = Math.Clamp(area.Left + shape.X, area.Left, Math.Max(area.Left, area.Right - size));
					var y = Math.Clamp(area.Top + shape.Y, area.Top, Math.Max(area.Top, area.Bottom - size));
					loaded.Add(new SquareState(shape.Id, new Rectangle(x, y, size, size), color.IsEmpty ? Color.Gray : color, kind));
				}

				_draggedSquare = null;
				_squares.Clear();
				_squares.AddRange(loaded);
				AppendStatus($"Preset chargé: {dialog.FileName} ({loaded.Count} forme(s)).");
				LogLine("PRESET_LOAD", $"File={dialog.FileName};Count={loaded.Count}");
				Invalidate();
			}
			catch (Exception ex)
			{
				AppendStatus($"Impossible de charger le preset: {ex.Message}");
				LogLine("ERROR", ex.ToString());
			}
		}
	}
}

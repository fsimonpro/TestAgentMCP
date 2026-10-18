namespace TestAgentMCP
{
	internal sealed class CanvasState
	{
		public int ClientWidth { get; set; }
		public int ClientHeight { get; set; }
		public int DrawingLeft { get; set; }
		public int DrawingTop { get; set; }
		public int DrawingWidth { get; set; }
		public int DrawingHeight { get; set; }
		public int FrameCount { get; set; } = 1;
		public int CurrentFrame { get; set; }
		public bool IsPlaying { get; set; }
		public int Fps { get; set; }
		public List<CanvasSquare> Squares { get; set; } = [];
	}

	internal sealed class CanvasSquare
	{
		public int Id { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int Size { get; set; }
		public string Color { get; set; } = "#808080";
		public string Shape { get; set; } = "Square";
	}

	internal sealed class AnimationRequest
	{
		public int? Fps { get; set; }
		public List<List<AgentMCP.SquareUpdate>>? Frames { get; set; }
	}
}

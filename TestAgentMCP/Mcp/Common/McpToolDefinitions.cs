namespace TestAgentMCP
{
	/// <summary>Définitions (nom, description, schéma) des outils exposés par <see cref="McpServer"/>.</summary>
	internal static class McpToolDefinitions
	{
		private static readonly string[] ShapeNames = ["Square", "Circle", "Losange"];

		private static readonly object ShapeInfoProperties = new
		{
			id = new { type = "integer", description = "Id de la forme (Update/Remove)." },
			x = new { type = "integer" },
			y = new { type = "integer" },
			size = new { type = "integer", description = "Côté/diamètre en pixels (défaut 60)." },
			color = new { type = "string", description = "Couleur HTML, ex. #FF0000 ou Red." },
			shape = new { type = "string", @enum = ShapeNames }
		};

		public static readonly object GetCanvasState = new
		{
			name = "get_canvas_state",
			description = "Retourne l'état actuel : zone de dessin (DrawingLeft/Top/Width/Height), formes {Id, X, Y, Size, Color, Shape} et état de l'animation. À appeler en premier. Voir get_help pour la documentation.",
			inputSchema = new { type = "object", properties = new { } }
		};

		public static readonly object ApplySquareUpdates = new
		{
			name = "apply_square_updates",
			description = "Ajoute, modifie ou supprime des formes sur le canvas, puis retourne le nouvel état. " +
				"Add: crée de nouvelles formes (id ignoré, attribué automatiquement ; position aléatoire si x/y omis). " +
				"Update: modifie les formes existantes par id (seuls les champs fournis changent). " +
				"Remove: supprime les formes dont l'id est donné.",
			inputSchema = new
			{
				type = "object",
				properties = new
				{
					action = new { type = "string", @enum = new[] { "Add", "Update", "Remove" } },
					infos = new
					{
						type = "array",
						items = new { type = "object", properties = ShapeInfoProperties }
					}
				},
				required = new[] { "action", "infos" }
			}
		};

		public static readonly object SetAnimation = new
		{
			name = "set_animation",
			description = "Définit une animation de 2 à 50 frames, jouée en boucle par l'app (15 fps par défaut) avec un lecteur " +
				"(slider, lecture/stop). Chaque frame est une liste de formes {id, x, y, size, color, shape} appliquée en Update sur la " +
				"frame précédente (la première part de l'état courant) : ne liste que ce qui change ; un id inconnu crée la forme. " +
				"frames vide: supprime l'animation et garde la frame affichée. Une seule frame: simple mise à jour.",
			inputSchema = new
			{
				type = "object",
				properties = new
				{
					fps = new { type = "integer", minimum = 1, maximum = 60, description = "Images par seconde (défaut 15)." },
					frames = new
					{
						type = "array",
						maxItems = 50,
						items = new
						{
							type = "array",
							items = new { type = "object", properties = ShapeInfoProperties }
						}
					}
				},
				required = new[] { "frames" }
			}
		};

		public static readonly object GetHelp = new
		{
			name = "get_help",
			description = "Retourne la documentation du canvas (règles, repère, animation, exemples). Topics: " + McpDocs.TopicList + ". Sans topic: toute la documentation.",
			inputSchema = new
			{
				type = "object",
				properties = new { topic = new { type = "string", @enum = McpDocs.All.Select(d => d.Topic).ToArray() } }
			}
		};

		public static readonly object[] All = [GetCanvasState, ApplySquareUpdates, SetAnimation, GetHelp];
	}
}

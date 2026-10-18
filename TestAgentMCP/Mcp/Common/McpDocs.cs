namespace TestAgentMCP
{
	/// <summary>Documentation exposée aux clients MCP : instructions d'initialisation, resources et outil get_help.</summary>
	internal static class McpDocs
	{
		public const string Instructions =
			"Canvas 2D de formes colorées (Square, Circle, Losange) que tu peux dessiner et animer.\n" +
			"1. Appelle toujours get_canvas_state d'abord : zone de dessin (DrawingLeft/Top/Width/Height), formes {Id, X, Y, Size, Color, Shape}, état de l'animation.\n" +
			"2. apply_square_updates modifie les formes (Add/Update/Remove) ; set_animation définit une animation en boucle.\n" +
			"3. Regroupe plusieurs formes en un seul appel. Les coordonnées sont ramenées dans la zone de dessin.\n" +
			"4. Pour le détail et des exemples, appelle get_help (topics: guide, animation, examples) ou lis les resources shapes://docs/*.";

		public sealed record Doc(string Topic, string Uri, string Title, string Text);

		public static readonly IReadOnlyList<Doc> All =
		[
			new("guide", "shapes://docs/guide", "Guide du canvas",
				"# Guide du canvas\n\n" +
				"- Repère : origine en haut à gauche, x vers la droite, y vers le bas, en pixels.\n" +
				"- La zone de dessin est donnée par get_canvas_state (DrawingLeft, DrawingTop, DrawingWidth, DrawingHeight). Une forme hors zone est ramenée dedans.\n" +
				"- (x, y) est le coin supérieur gauche de la forme ; size est le côté (Square), le diamètre (Circle) ou la largeur et hauteur du losange (inscrit dans le carré de côté size) ; taille par défaut 60.\n" +
				"- shape : Square, Circle ou Losange. color : couleur HTML (#FF0000, Red).\n\n" +
				"## apply_square_updates\n" +
				"- Add : l'id est ignoré (attribué automatiquement) ; x/y omis = position aléatoire.\n" +
				"- Update : seuls les champs fournis changent ; l'id doit exister.\n" +
				"- Remove : seul l'id compte.\n" +
				"- L'appel retourne le nouvel état complet : utilise-le pour vérifier le résultat et récupérer les ids attribués.\n\n" +
				"## Conseils\n" +
				"- Les formes sont dessinées dans l'ordre de création : les plus récentes sont au-dessus.\n" +
				"- Pour une scène, prépare d'abord la composition (fond, éléments, détails) puis envoie tout en un Add.\n" +
				"- Pour effacer tout le canvas, fais un Remove de tous les ids lus dans get_canvas_state."),

			new("animation", "shapes://docs/animation", "Animation",
				"# Animation (set_animation)\n\n" +
				"- frames : liste de 2 à 50 frames ; chaque frame est une liste de formes {id, x, y, size, color, shape}.\n" +
				"- Chaque frame s'applique en Update sur la précédente ; la première part de l'état courant. Ne liste donc que ce qui change.\n" +
				"- Un id inconnu crée la forme. fps optionnel (1 à 60, défaut 15). Lecture en boucle.\n" +
				"- frames: [] supprime l'animation et garde la frame affichée. Une seule frame = simple mise à jour.\n" +
				"- get_canvas_state expose FrameCount, CurrentFrame, IsPlaying, Fps.\n\n" +
				"## Conseils\n" +
				"- Ajoute d'abord les formes avec apply_square_updates (pour connaître leurs ids), puis anime-les par id.\n" +
				"- Mouvement fluide : déplace de quelques pixels par frame ; 20 à 40 frames suffisent pour une boucle.\n" +
				"- Boucle propre : la dernière frame doit ramener les formes vers leur position de départ.\n" +
				"- Effets possibles : déplacement (x, y), pulsation (size), clignotement ou dégradé (color), transformation (shape)."),

			new("examples", "shapes://docs/examples", "Exemples",
				"# Exemples\n\n" +
				"## Soleil et sol\n" +
				"1. get_canvas_state pour lire les limites.\n" +
				"2. apply_square_updates Add : [{x: 0, y: DrawingHeight-80, size: 80, color: \"Green\", shape: \"Square\"} répété sur la largeur, {x: 100, y: 40, size: 90, color: \"Gold\", shape: \"Circle\"}].\n\n" +
				"## Balle qui rebondit\n" +
				"1. Add d'un Circle rouge ; note son id.\n" +
				"2. set_animation avec ~20 frames [{id, y: …}] dont y descend puis remonte, fps 20.\n\n" +
				"## Changement de couleur\n" +
				"set_animation avec des frames [{id, color: \"#FF0000\"}], [{id, color: \"#FF8800\"}], [{id, color: \"#FFFF00\"}]…")
		];

		public static Doc? Find(string? topic) =>
			All.FirstOrDefault(d => string.Equals(d.Topic, topic, StringComparison.OrdinalIgnoreCase));

		public static string TopicList => string.Join(", ", All.Select(d => d.Topic));
	}
}

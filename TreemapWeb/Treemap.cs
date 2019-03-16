using System.Windows;
using System.Collections.Generic;
using System;
using System.IO;

namespace Treemap {
	public struct Rect {
		public double Width, Height;
		public double X, Y;
		public Rect (double x, double y, double w, double h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}
	}

	public class Node : IComparer<Node> {
		public string Name;

		// The weight
		public int Size;
		public int Value;

		// Children of this node
		public List<Node> Children;

		// Used during layout: 
		public int Area;
		public Rect Rect;

		public int Compare (Node a, Node b)
		{
			int n = b.Size - a.Size;
			if (n != 0)
				return n;
			return string.Compare (b.Name, a.Name);
		}

		public Node Clone ()
		{
			return new Node (this);
		}

		public Node ()
		{
			Children = new List<Node> ();
		}

		public Node (int n)
		{
			Children = new List<Node> (n);
		}

		public Node (Node re)
		{
			Name = re.Name;
			Size = re.Size;
			Area = re.Area;
			Rect = re.Rect;

			Children = new List<Node> (re.Children.Count);
			foreach (Node rec in re.Children)
				Children.Add (rec);

		}
	}

	public class TreemapRenderer {
		StreamWriter output;
		Node root;
		string caption;
		Rect region;

		void p (string fmt, params object [] args)
		{
			output.WriteLine (string.Format (fmt, args));
		}

		public TreemapRenderer (Node source, string caption, StreamWriter output)
		{
			this.root = source.Clone ();
			this.caption = caption;
			this.output = output;

			Sort (root);
		}

		public void SetRegion (Rect newRegion)
		{
			region = newRegion;

			Rect emptyArea = region;
			Squarify (emptyArea, root.Children);

			Plot (root.Children);
		}

		const int PADX = 5;
		const int PADY = 3;

		void Plot (List<Node> children, int l = 0)
		{
			foreach (Node child in children) {
				var vis = l > 0 ? $"visibility: hidden" : "visibility: visible";
				var location = $"position: fixed; top: {child.Rect.Y}%; left: {child.Rect.X}%; width:{child.Rect.Width}%; height: {child.Rect.Height}%";
				var pctw = child.Rect.Width;
				var size = pctw > 50 ? "" : (pctw > 30 ? "-medium" : (pctw < 20 ? "-tiny" : "-small"));

				p ($"<div class=\"node\" style=\"{vis}; {location};\">");
				p ($"<div class=\"text-wrapper{size}\">{child.Name}");
					if (child.Children.Count > 0) {
						Squarify (new Rect (0, 0, 100, 100), child.Children);
						Plot (child.Children, l + 1);
					}
					if (child.Rect.Height > 20) {
						p ($"<div class=\"stats\">Bytes: {child.Size:n0}</div>");
					}
				p ($"</div>");
				p ("</div>");
			}
		}

		static string MakeCaption (string s, out int max)
		{
			string [] elements = s.Split (new char [] { '.' });

			max = 0;
			foreach (string el in elements)
				if (el.Length > max)
					max = el.Length;
			return string.Join ("\n", elements);
		}

		public static double GetShortestSide (Rect r)
		{
			return Math.Min (r.Width, r.Height);
		}

		static void Squarify (Rect emptyArea, List<Node> children)
		{
			double fullArea = 0;
			foreach (Node child in children) {
				fullArea += child.Size;
			}

			double area = emptyArea.Width * emptyArea.Height;
			foreach (Node child in children) {
				child.Area = (int)(area * child.Size / fullArea);
			}

			Squarify (emptyArea, children, new List<Node> (), GetShortestSide (emptyArea));

			foreach (Node child in children) {
				if (child.Area < 9000 || child.Children.Count == 0) {
					//Console.WriteLine ("Passing on this {0} {1} {2}", child.Area, child.Children, child.Children.Count);
					continue;
				}

				Squarify (child.Rect, child.Children);
			}
		}

		static void Squarify (Rect emptyArea, List<Node> children, List<Node> row, double w)
		{
			if (children.Count == 0) {
				AddRowToLayout (emptyArea, row);
				return;
			}

			Node head = children [0];

			List<Node> row_plus_head = new List<Node> (row);
			row_plus_head.Add (head);

			double worst1 = Worst (row, w);
			double worst2 = Worst (row_plus_head, w);

			if (row.Count == 0 || worst1 > worst2) {
				List<Node> children_tail = new List<Node> (children);
				children_tail.RemoveAt (0);
				Squarify (emptyArea, children_tail, row_plus_head, w);
			} else {
				emptyArea = AddRowToLayout (emptyArea, row);
				Squarify (emptyArea, children, new List<Node> (), GetShortestSide (emptyArea));
			}
		}

		static double Worst (List<Node> row, double sideLength)
		{
			if (row.Count == 0)
				return 0;

			double maxArea = 0, minArea = double.MaxValue;
			double totalArea = 0;
			foreach (Node n in row) {
				maxArea = Math.Max (maxArea, n.Area);
				minArea = Math.Min (minArea, n.Area);
				totalArea += n.Area;
			}

			if (minArea == double.MaxValue)
				minArea = 0;

			double v1 = (sideLength * sideLength * maxArea) / (totalArea * totalArea);
			double v2 = (totalArea * totalArea) / (sideLength * sideLength * minArea);

			return Math.Max (v1, v2);
		}

		static Rect AddRowToLayout (Rect emptyArea, List<Node> row)
		{
			Rect result;
			double areaUsed = 0;
			foreach (Node n in row)
				areaUsed += n.Area;

			if (emptyArea.Width > emptyArea.Height) {
				double w = areaUsed / emptyArea.Height;
				result = new Rect (emptyArea.X + w, emptyArea.Y, Math.Max (0, emptyArea.Width - w), emptyArea.Height);

				double y = emptyArea.Y;
				foreach (Node n in row) {
					double h = n.Area * emptyArea.Height / areaUsed;

					n.Rect = new Rect (emptyArea.X, y, w, h);
					//Console.WriteLine ("       PLACE Item {0}->{1}", n.Name, n.Rect);
					//Console.WriteLine ("Slot {0} with {1} got {2}", n.Name, n.Size, n.Rect);
					y += h;
				}
			} else {
				double h = areaUsed / emptyArea.Width;
				//Console.WriteLine ("   Height > Width: {0}", h);
				result = new Rect (emptyArea.X, emptyArea.Y + h, emptyArea.Width, Math.Max (0, emptyArea.Height - h));

				double x = emptyArea.X;
				foreach (Node n in row) {
					double w = n.Area * emptyArea.Width / areaUsed;
					n.Rect = new Rect (x, emptyArea.Y, w, h);
					x += w;
				}
			}

			return result;
		}

		static void Sort (Node n)
		{
			n.Children.Sort (n);
			foreach (Node child in n.Children)
				Sort (child);
		}
	}

}
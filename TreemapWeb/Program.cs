// 
// Renders the treemap as HTML
// Authors:
//    Miguel de Icaza (miguel.de.icaza@gmail.com)
//
// 
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Treemap {
	class MainClass {
		public static void Main (string [] args)
		{
			Console.WriteLine ("Hello World!");
			var n = LoadNodes ("/tmp/x", "Size", "Second");
			var output = File.CreateText ("/tmp/out.html");
			var path = "/Users/miguel/Dropbox/TreemapWeb/TreemapWeb";
			output.WriteLine ($"<html><script src=\"{path}/tree.js\"></script><link href=\"{path}/treemap.css\" rel=\"stylesheet\"></link>");
			output.WriteLine ("<body>");
			var t = new TreemapRenderer (n.Children.First (), "Home", output);
			t.SetRegion (new Rect (0, 0, 100, 100));
			//DumpNode(n, 0);
		}

		static void DumpNode (Node n, int indent)
		{
			for (int i = 0; i < indent; i++)
				Console.Write (" ");
			Console.WriteLine ("{0} => {1}", n.Name, n.Size);
			foreach (Node child in n.Children) {
				DumpNode (child, (indent + 1) * 2);
			}
		}

		static XName xn_name = XName.Get ("Name", "");

		public static Node LoadNodes (string s, string dim1, string dim2)
		{
			XDocument d = XDocument.Load (s);
			XElement root = ((XElement)d.FirstNode);
			XName d1 = XName.Get (dim1, "");
			XName d2 = XName.Get (dim2, "");

			return LoadNodes (root, d1, d2);
		}

		public static void Update (XElement xe, ref int v, XName k)
		{

			var attr = xe.Attribute (k);
			if (attr != null) {
				if (int.TryParse (attr.Value, out int r))
					v += r;
			}
		}


		public static Node LoadNodes (XElement xe, XName k1, XName k2)
		{
			XAttribute xa = xe.Attribute (xn_name);

			Node n = new Node (xe.Nodes ().Count ());
			if (xa != null)
				n.Name = xa.Value;

			Update (xe, ref n.Size, k1);
			Update (xe, ref n.Value, k2);

			foreach (XNode e in xe.Nodes ()) {
				if (e is XElement) {
					Node child = LoadNodes ((XElement)e, k1, k2);
					n.Size += child.Size;
					n.Value += child.Value;

					n.Children.Add (child);
				}
			}

			return n;
		}

	}
}

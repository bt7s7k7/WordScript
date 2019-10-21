﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordScript;

namespace WordScriptREPL {
	class Program {
		static string file = "<anonymous>";
		static void Main(string[] args) {
			if (args.Length == 0) {
				Console.WriteLine("WordScript REPL\nCopyright (C) Branislav Trstenský 2019\n");
				while (true) {
					Console.Write("> ");
					var input = Console.ReadLine();
					Run(input);
				}
			} else {
				file = Path.GetFileName(args[0]);
				Run(File.ReadAllText(args[0]));
			}
		}

		static void Run(string code) {
			List<CodeTokenizer.Token> tokens = null;
			try {
				tokens = CodeTokenizer.Tokenize(code,file);
			} catch (Exception ex) {
				Console.WriteLine(ex.StackTrace);
			}
			if (tokens != null) {
				foreach (var token in tokens) {
					Console.WriteLine(token.ToString());
				}
			}
		}
	}
}
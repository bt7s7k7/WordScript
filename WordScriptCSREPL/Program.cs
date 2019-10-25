using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordScript;

namespace WordScriptREPL {
	class Program {

		[FunctionDefinition("print")]
		public static void Print(string s) => Console.WriteLine(s);

		static string file = "<anonymous>";
		static void Main(string[] args) {
			enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			if (args.Length == 0 || args[0][0] == '#') {
				Console.WriteLine("WordScript REPL\nCopyright (C) Branislav Trstenský 2019\n");
				while (true) {
					Console.Write("> ");
					var input = Console.ReadLine();
					Run(input);
				}
			} else {
				file = Path.GetFileName(args[0]);
				Run(File.ReadAllText(args[0]));
				Console.Read();
			}
		}
		static Enviroment enviroment = null;
		static void Run(string code) {
			List<CodeTokenizer.Token> tokens = null;
			StatementBlock block = null;
			try {
				tokens = CodeTokenizer.Tokenize(code,file);
				var tokensEnum = (IEnumerator<CodeTokenizer.Token>)tokens.GetEnumerator();
				block = TokenParser.Parse(ref tokensEnum, enviroment, CodePosition.GetExternal());
			} catch (WordScriptException ex) {
				Console.WriteLine(ex.Message);
			}
			if (tokens != null) {
				foreach (var token in tokens) {
					Console.WriteLine(token.ToString());
				}
			}
			Console.WriteLine("");
			if (block != null) {
				foreach (var node in block.GetSyntaxNodes()) {
					Console.WriteLine(node.Debug());
				}

				var ret = block.Evaluate();

				Console.WriteLine(ret?.ToString() ?? "null");
			}
		}
	}
}

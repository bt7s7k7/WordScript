using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;

namespace WordScript {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class TypeNameAttribute : Attribute {
		public Type targetType;

		public TypeNameAttribute(Type targetType) {
			this.targetType = targetType;
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class TypeConversionAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public sealed class FunctionDefinitionAttribute : Attribute {
		string name;

		public FunctionDefinitionAttribute(string name) {
			this.name = name;
		}
	}

	public struct Function {
		public Func<object[], object> function;
		public Type returnType;
		public Type[] arguments;
		public string name;
	}


	[Serializable]
	public class TypeNotRegisteredException : Exception {
		public TypeNotRegisteredException() { }
		public TypeNotRegisteredException(string message) : base(message) { }
		public TypeNotRegisteredException(string message, Exception inner) : base(message, inner) { }
		protected TypeNotRegisteredException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}


	[Serializable]
	public class FunctionNotFoundException : Exception {
		public FunctionNotFoundException() { }
		public FunctionNotFoundException(string message) : base(message) { }
		public FunctionNotFoundException(string message, Exception inner) : base(message, inner) { }
		protected FunctionNotFoundException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class TokenizationException : Exception {
		public TokenizationException() { }
		public TokenizationException(string message) : base(message) { }
		public TokenizationException(string message, Exception inner) : base(message, inner) { }
		protected TokenizationException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class EndOfFileException : TokenizationException {
		public EndOfFileException() { }
		public EndOfFileException(string message) : base(message) { }
		public EndOfFileException(string message, Exception inner) : base(message, inner) { }
		protected EndOfFileException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class UnknownEscapeCharacterException : TokenizationException {
		public UnknownEscapeCharacterException() { }
		public UnknownEscapeCharacterException(string message) : base(message) { }
		public UnknownEscapeCharacterException(string message, Exception inner) : base(message, inner) { }
		protected UnknownEscapeCharacterException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}


	public class TypeInfoProvider {
		private static Dictionary<Type, string> names;
		private static Dictionary<string, Function> functions;
		private static Dictionary<string, List<string>> overloads;

		protected void AddOverload(string name, string signature) {
			if (overloads.TryGetValue(name, out List<string> signatures)) {
				signatures.Add(signature);
			} else {
				overloads.Add(name, new List<string> { signature });
			}
		}

		public void AddFunction(Function func) {
			string signature = GetFunctionSignature(func.name, func.arguments);
			functions.Add(signature, func);
			AddOverload(func.name, signature);
		}

		public string GetFunctionSignature(string name, Type[] arguments) {
			return name + " " + string.Join(" ", arguments.Select(v => names[v]));
		}

		public string GetTypeName(Type type) {
			if (names.TryGetValue(type, out string name)) {
				return name;
			} else {
				throw new TypeNotRegisteredException("Type " + name + " is not registered");
			}
		}

		public Function GetFunction(string name) {
			if (functions.TryGetValue(name, out Function function)) {
				return function;
			} else throw new FunctionNotFoundException("Function " + name + " was not found");
		}

		public List<string> GetOverloads(string name) {
			if (overloads.TryGetValue(name, out List<string> signatures)) {
				return signatures;
			} else {
				return new List<string> { };
			}
		}

		public TypeInfoProvider() {
			if (names == null) {
				names = new Dictionary<Type, string>();
				functions = new Dictionary<string, Function>();
				overloads = new Dictionary<string, List<string>>();
				var methods = AppDomain.CurrentDomain.GetAssemblies().AsParallel()
					.SelectMany((v) => v.GetTypes())
					.SelectMany(v => v.GetMethods())
					.Where(v => v.IsStatic && !v.IsGenericMethodDefinition && !v.IsGenericMethod);

				foreach (var method in methods) {
					var nameAttr = method.GetCustomAttribute<TypeNameAttribute>(false);
					if (nameAttr != null) {
						var name = (string)method.Invoke(null, new object[] { });

						names.Add(nameAttr.targetType, name);
					}
				}

				foreach (var method in methods) {
					var convAttr = method.GetCustomAttribute<TypeConversionAttribute>(false);
					if (convAttr != null) {
						var source = method.GetParameters()[0]?.ParameterType;
						if (source == null) throw new Exception("Conversion method does not accept a parameter");
						var target = method.ReturnType;

						AddFunction(new Function {
							name = GetTypeName(target) + ".from",
							arguments = new Type[] { source },
							returnType = target,
							function = (args) => method.Invoke(null, args)
						});
					}
				}

				foreach (var method in methods) {
					var funcDefAttrs = method.GetCustomAttributes<FunctionDefinitionAttribute>(false);
					foreach (var funcDefAttr in funcDefAttrs) {
						var returnType = method.ReturnType;
						var arguments = method.GetParameters().Select(v => v.ParameterType);
					}
				}

			}


		}
	}

	public static class DefaultTypeInfo {
		[TypeName(typeof(string))]
		public static string GetStringName() => "string";

		[TypeName(typeof(int))]
		public static string GetIntName() => "int";
	}

	public struct CodePosition {
		public int line;
		public int col;
		public string file;

		public CodePosition(string file) : this() {
			this.file = file;
		}

		public void NextLetter() => col++;
		public void NextLine() {
			col = 0;
			line++;
		}
		public override string ToString() {
			return "at " + line + ":" + col + ":" + file;
		}
	}

	public static class CodeTokenizer {
		public struct Token {
			public enum Type {
				Word,
				Pipe,
				Terminator,
				Inline,
				StringLiteral
			}

			public Type type;
			public string text;
			public CodePosition position;

			public static Token MakeTerminator(string text, CodePosition position) => new Token { text = text, type = Type.Terminator, position = position };
			public static Token MakePipe(string text, CodePosition position) => new Token { text = text, type = Type.Pipe, position = position };
			public static Token MakeWord(string text, CodePosition position) => new Token { text = text, type = Type.Word, position = position };
			public static Token MakeInline(string text, CodePosition position) => new Token { text = text, type = Type.Inline, position = position };
			public static Token MakeStringLiteral(string text, CodePosition position) => new Token { text = text, type = Type.StringLiteral, position = position };

			public override string ToString() {
				return "\"" + text + "\":" + type.ToString() + " " + position.ToString();
			}
		}

		public static List<Token> Tokenize(string code, string file = "<anonymous>") {
			List<Token> ret = new List<Token>();
			int position = 0;

			CodePosition codePosition = new CodePosition(file);

			while (true) {
				if (position >= code.Length) {
					break;
				}
				int startPos = position;
				bool isOver = false;
				bool isString = false;
				if (char.IsWhiteSpace(code[position])) {
					position++;
					codePosition.NextLetter();
					if (position >= code.Length) {
						break;
					}
					continue;
				}

				if (code[position] == '"' || code[position] == '\'') {
					bool startedWithAp = code[position] == '\'';
					isString = true;
					position++;
					codePosition.NextLetter();
					bool isEscape = false;
					System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
					while (true) {
						if (position >= code.Length) {
							throw new EndOfFileException("Unexpected end of file " + codePosition.ToString());
						};

						if (isEscape) {
							if (code[position] == 'n') stringBuilder.Append("\n");
							if (code[position] == '\\') stringBuilder.Append("\\");
							if (code[position] == 'b') stringBuilder.Append("\b");
							if (code[position] == 'r') stringBuilder.Append("\r");
							if (code[position] == '\'') stringBuilder.Append("'");
							if (code[position] == '"') stringBuilder.Append("\"");
							else throw new UnknownEscapeCharacterException("Unknown escape character \\" + code[position]);
						} else {
							if (code[position] == '\\') {
								isEscape = true;
								stringBuilder.Append(code, startPos, position - startPos);
								startPos = position;
							} else if (code[position] == (startedWithAp ? '\'' : '"')) {
								stringBuilder.Append(code, startPos, position - startPos);
								position++;
								codePosition.NextLetter();
								if (code[position] == '\n') codePosition.NextLine();
								break;
							}
						}

						position++;
						codePosition.NextLetter();
						if (code[position] == '\n') codePosition.NextLine();
					}
				} else {
					while (true) {
						position++;
						codePosition.NextLetter();
						if (position >= code.Length) {
							isOver = true;
						} else if (code[position] == '\n') codePosition.NextLine();

						if (isOver || char.IsWhiteSpace(code[position])) {
							break;
						}
					}
				}

				string word = code.Substring(startPos, position - startPos);
				if (!isString) {
					word = word.Trim();
					if (word.Length == 0) continue;
				} else word = word.Substring(1, word.Length - 2);

				if (word == ".") {
					ret.Add(Token.MakeTerminator(word, codePosition));
				} else if (word == ",") {
					ret.Add(Token.MakePipe(word, codePosition));
				} else if (word == "IN") {
					ret.Add(Token.MakeInline(word, codePosition));
				} else if (isString) {
					ret.Add(Token.MakeStringLiteral(word, codePosition));
				} else {
					ret.Add(Token.MakeWord(word, codePosition));
				}
			}

			return ret;

		}
	}


}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Text;

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

	public class Function {
		public Func<object[], object> function;
		public Type returnType;
		public Type[] arguments;
		public string name;
	}


	[Serializable]
	public class WordScriptException : Exception {
		public WordScriptException() { }
		public WordScriptException(string message) : base(message) { }
		public WordScriptException(string message, Exception inner) : base(message, inner) { }
		protected WordScriptException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class TypeNotRegisteredException : WordScriptException {
		public TypeNotRegisteredException() { }
		public TypeNotRegisteredException(string message) : base(message) { }
		public TypeNotRegisteredException(string message, Exception inner) : base(message, inner) { }
		protected TypeNotRegisteredException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}


	[Serializable]
	public class FunctionNotFoundException : WordScriptException {
		public FunctionNotFoundException() { }
		public FunctionNotFoundException(string message) : base(message) { }
		public FunctionNotFoundException(string message, Exception inner) : base(message, inner) { }
		protected FunctionNotFoundException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class TokenizationException : WordScriptException {
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


	[Serializable]
	public class UnexpectedTokenException : WordScriptException {
		public UnexpectedTokenException() { }
		public UnexpectedTokenException(string message) : base(message) { }
		public UnexpectedTokenException(string message, Exception inner) : base(message, inner) { }
		protected UnexpectedTokenException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}


	[Serializable]
	public class NumberLiteralException : WordScriptException {
		public NumberLiteralException() { }
		public NumberLiteralException(string message) : base(message) { }
		public NumberLiteralException(string message, Exception inner) : base(message, inner) { }
		protected NumberLiteralException(
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

		public string GetFunctionSignature(string name, IEnumerable<Type> arguments) {
			return name + " " + string.Join(" ", arguments.Select(v => GetTypeName(v)));
		}

		public string GetTypeName(Type type) {
			if (names.TryGetValue(type, out string name)) {
				return name;
			} else {
				throw new TypeNotRegisteredException("Type " + type.FullName + " is not registered");
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

		[TypeName(typeof(float))]
		public static string GetFloatName() => "float";

		[TypeConversion]
		public static string IntToString(int v) => v.ToString();

		[TypeConversion]
		public static string FloatToString(float v) => v.ToString();
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
				StringBuilder stringBuilder = null;
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
					stringBuilder = new System.Text.StringBuilder();
					while (true) {
						if (position >= code.Length) {
							throw new EndOfFileException("Unexpected end of file, expected string literal end " + codePosition.ToString());
						};

						if (isEscape) {
							if (code[position] == 'n') stringBuilder.Append("\n");
							else if (code[position] == '\\') stringBuilder.Append("\\");
							else if (code[position] == 'b') stringBuilder.Append("\b");
							else if (code[position] == 'r') stringBuilder.Append("\r");
							else if (code[position] == '\'') stringBuilder.Append("'");
							else if (code[position] == '"') stringBuilder.Append("\"");
							else throw new UnknownEscapeCharacterException("Unknown escape character \\" + code[position]);

							isEscape = false;
						} else {
							if (code[position] == '\\') {
								isEscape = true;
								stringBuilder.Append(code, startPos, position - startPos);
								startPos = position + 2;
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

				string word = stringBuilder?.ToString() ?? code.Substring(startPos, position - startPos);
				if (!isString) {
					word = word.Trim();
					if (word.Length == 0) continue;
				} else word = word.Substring(1);

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

	public class TokenParser {

		public static SyntaxNode ParseStatement(ref IEnumerator<CodeTokenizer.Token> enumerator, bool isArgument, SyntaxNode piped = null) {
			SyntaxNode ret = null;
			SyntaxNodeTypes.Statement statement = null;
			CodeTokenizer.Token currentToken = enumerator.Current;
			if (
				currentToken.type == CodeTokenizer.Token.Type.Terminator
				|| currentToken.type == CodeTokenizer.Token.Type.Pipe
			) {
				throw new UnexpectedTokenException("Expected statement, unexpected " + currentToken.ToString());
			} else if (currentToken.type == CodeTokenizer.Token.Type.Inline) {
				// If inline, parse the inline statement and return it
				if (!isArgument) throw new UnexpectedTokenException("Expected statement start, unexpected " + currentToken.position.ToString());
				// Move next to the start of the inline statement
				if (!enumerator.MoveNext()) throw new EndOfFileException("Unexpected end of file, expected statement " + currentToken.position.ToString());
				return ParseStatement(ref enumerator, false);
			} else if (currentToken.type == CodeTokenizer.Token.Type.StringLiteral) {
				if (piped != null) throw new UnexpectedTokenException("Cannot pipe into a string " + currentToken.position.ToString());
				ret = new SyntaxNodeTypes.Literal<string>(currentToken.text, currentToken.position);
			} else if (currentToken.type == CodeTokenizer.Token.Type.Word) {
				if (char.IsDigit(currentToken.text[0])) {
					if (piped != null) throw new UnexpectedTokenException("Cannot pipe into a number " + currentToken.position.ToString());
					var last = currentToken.text[currentToken.text.Length - 1];
					try {
						if (!char.IsDigit(last)) {
							if (last == 'i') {
								ret = new SyntaxNodeTypes.Literal<int>(int.Parse(currentToken.text), currentToken.position);
							} else if (last == 'f') {
								ret = new SyntaxNodeTypes.Literal<float>(float.Parse(currentToken.text), currentToken.position);
							} else throw new NumberLiteralException("Unknown number type '" + last + "' " + currentToken.position);
						} else {
							ret = new SyntaxNodeTypes.Literal<int>(int.Parse(currentToken.text), currentToken.position);
						}
					} catch (FormatException ex) {
						throw new NumberLiteralException("Number is not of correct format " + currentToken.position);
					}
				} else {
					statement = new SyntaxNodeTypes.Statement(currentToken.position);
					ret = statement;
				}
			}

			if (isArgument) {
				if (statement != null) statement.Validate(currentToken.text);
				return ret;
			}

			if (piped != null) {
				if (statement != null) {
					statement.children.Add(piped);
				} else {
					throw new UnexpectedTokenException("Unexpected literal, expected a statement to pipe into " + currentToken.position.ToString());
				}
			}

			while (true) {
				{
					var lastPos = enumerator.Current.position;
					if (!enumerator.MoveNext()) {
						throw new EndOfFileException("Unexpected end of file, expected a argument, if statement end expected terminator" + lastPos);
					}
				}

				var current = enumerator.Current;
				if (current.type == CodeTokenizer.Token.Type.Terminator) {
					if (statement != null) statement.Validate(currentToken.text);
					return ret;
				} else if (current.type == CodeTokenizer.Token.Type.Pipe) {
					if (statement != null) statement.Validate(currentToken.text);
					var lastPos = enumerator.Current.position;
					if (!enumerator.MoveNext()) {
						throw new EndOfFileException("Unexpected end of file, expected a statement to pipe into" + lastPos);
					}
					return ParseStatement(ref enumerator, false, ret);
				} else {
					if (statement != null) {
						statement.children.Add(ParseStatement(ref enumerator, true));
					} else {
						throw new UnexpectedTokenException("Unexpected argument, expected a terminator " + current.position);
					}
				}
			}
		}

		public static List<SyntaxNode> Parse(List<CodeTokenizer.Token> tokens) {
			var ret = new List<SyntaxNode>();
			IEnumerator<CodeTokenizer.Token> enumerator = tokens.GetEnumerator();

			while (enumerator.MoveNext()) {
				ret.Add(ParseStatement(ref enumerator, false));
			}

			return ret;
		}

		public static List<SyntaxNode> Parse(string text) {
			return Parse(CodeTokenizer.Tokenize(text));
		}
	}

	public abstract class SyntaxNode {
		public abstract object Evaluate();
		public abstract Type GetReturnType();

		public abstract void GetDebug(Action<string> write, ref int indent);

		public string Debug() {
			StringBuilder builder = new StringBuilder();
			int indent = 0;
			GetDebug((s) => {
				builder.Append(new string(' ', indent * 2));
				builder.Append(s);
				builder.Append("\n");
			}, ref indent);

			return builder.ToString();
		}

		public CodePosition position;
	}

	namespace SyntaxNodeTypes {
		public class Statement : SyntaxNode {
			public List<SyntaxNode> children = new List<SyntaxNode>();
			public Function function = null;

			public Statement(CodePosition position) {
				this.position = position;
			}

			public override object Evaluate() {
				if (function == null) {
					throw new Exception("Statement not validated yet");
				} else {
					throw new NotImplementedException("Evaluation not yet implemented");
				}
			}

			public override Type GetReturnType() {
				return function?.returnType ?? throw new Exception("Statement not validated yet");
			}

			public void Validate(string name) {
				if (function != null) throw new Exception("Tryied to validate a validated statement");
				var provider = new TypeInfoProvider();
				var overloads = provider.GetOverloads(name);
				var signature = provider.GetFunctionSignature(name, children.Select(v => v.GetReturnType()));
				var signIndex = overloads.IndexOf(signature);
				if (signIndex == -1) {
					throw new FunctionNotFoundException("Function not found \"" + signature + "\"" + position.ToString() + ", posible overloads: {\n  " + string.Join(", \n  ", overloads) + "\n}");
				} else {
					function = provider.GetFunction(signature);
				}
			}

			public override string ToString() {
				return base.ToString() + "[" + children.Count + "]";
			}

			public override void GetDebug(Action<string> write, ref int indent) {
				write("Statment[" + children.Count + "] = {");
				indent++;
				foreach (var child in children) {
					child.GetDebug(write, ref indent);
				}
				indent--;
				write("}");
			}
		}

		public class Literal<T> : SyntaxNode {
			public T value;

			public Literal(T value, CodePosition position) {
				this.value = value;
				this.position = position;
			}

			public override object Evaluate() {
				return value;
			}

			public override Type GetReturnType() {
				return typeof(T);
			}

			public override void GetDebug(Action<string> write, ref int indent) {
				write(typeof(T).FullName + " " + value.ToString());
			}
		}
	}


}
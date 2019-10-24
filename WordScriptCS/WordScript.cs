using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Text;
using System.Globalization;

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
		public string name;

		public FunctionDefinitionAttribute(string name) {
			this.name = name;
		}
	}

	public class Function {
		public Func<object[], object> function;
		public Type returnType;
		public IEnumerable<Type> arguments;
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


	[Serializable]
	public class VariableException : WordScriptException {
		public VariableException() { }
		public VariableException(string message) : base(message) { }
		public VariableException(string message, Exception inner) : base(message, inner) { }
		protected VariableException(
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
							name = GetTypeName(target),
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

						var function = new Function {
							arguments = arguments,
							name = funcDefAttr.name,
							function = (args) => method.Invoke(null, args),
							returnType = returnType
						};

						AddFunction(function);
					}
				}

			}


		}

		public Type GetTypeByName(string name) {
			var retCan = names.Where(v => v.Value == name).Select(v => v.Key);
			if (retCan.Count() < 1) throw new TypeNotRegisteredException("There is no type registered for name " + name);
			else return retCan.First();
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

		[TypeConversion]
		public static int FloatToInt(float v) => (int)Math.Floor(v);

		[TypeConversion]
		public static float IntToFloat(int v) => v;

		[TypeConversion]
		public static int StringToInt(string v) => int.Parse(v);

		[TypeConversion]
		public static float StringToFloat(string v) => float.Parse(v, CultureInfo.InvariantCulture);

		[FunctionDefinition("print")]
		public static void Print(string v) => Enviroment.printFunction(v);

		[FunctionDefinition("add")]
		public static string AddString(string a, string b) => a + b;

		[FunctionDefinition("add")]
		public static int AddInt(int a, int b) => a + b;
		
		[FunctionDefinition("add")]
		public static float AddFloat(float a, float b) => a + b;
		
		[FunctionDefinition("sub")]
		public static int SubInt(int a, int b) => a - b;
		
		[FunctionDefinition("sub")]
		public static float SubFloat(float a, float b) => a - b;
		
		[FunctionDefinition("mul")]
		public static int MulInt(int a, int b) => a * b;
		
		[FunctionDefinition("mul")]
		public static float MulFloat(float a, float b) => a * b;
		
		[FunctionDefinition("div")]
		public static int DivInt(int a, int b) => a / b;
		
		[FunctionDefinition("div")]
		public static float DivFloat(float a, float b) => a / b;

	}

	public struct CodePosition {
		public int line;
		public int col;
		public string file;

		public CodePosition(string file) : this() {
			this.file = file;
		}

		public override bool Equals(object obj) {
			return obj is CodePosition position &&
				   line == position.line &&
				   col == position.col &&
				   file == position.file;
		}

		public override int GetHashCode() {
			var hashCode = -2131546509;
			hashCode = hashCode * -1521134295 + line.GetHashCode();
			hashCode = hashCode * -1521134295 + col.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(file);
			return hashCode;
		}

		public void NextLetter() => col++;
		public void NextLine() {
			col = 0;
			line++;
		}
		public override string ToString() {
			return "at " + line + ":" + col + ":" + file;
		}

		public static bool operator ==(CodePosition a, CodePosition b) {
			return a.line == b.line && a.col == b.col && a.file == b.file;
		}

		public static bool operator !=(CodePosition a, CodePosition b) {
			return !(a == b);
		}

		public static CodePosition GetExternal() => new CodePosition("<external>");
	}

	public static class CodeTokenizer {
		public struct Token {
			public enum Type {
				Word,
				Pipe,
				Terminator,
				Keyword,
				StringLiteral
			}

			public Type type;
			public string text;
			public CodePosition position;

			public static Token MakeTerminator(string text, CodePosition position) => new Token { text = text, type = Type.Terminator, position = position };
			public static Token MakePipe(string text, CodePosition position) => new Token { text = text, type = Type.Pipe, position = position };
			public static Token MakeWord(string text, CodePosition position) => new Token { text = text, type = Type.Word, position = position };
			public static Token MakeKeyword(string text, CodePosition position) => new Token { text = text, type = Type.Keyword, position = position };
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
				} else if (word.Where(v => char.IsLetter(v) && char.ToUpper(v) == v).Count() == word.Length) {
					ret.Add(Token.MakeKeyword(word, codePosition));
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

		public static SyntaxNode ParseStatement(ref IEnumerator<CodeTokenizer.Token> enumerator, Enviroment enviroment, bool isArgument, SyntaxNode piped = null) {
			SyntaxNode ret = null;
			SyntaxNodeTypes.Statement statement = null;
			CodeTokenizer.Token currentToken = enumerator.Current;
			if (
				currentToken.type == CodeTokenizer.Token.Type.Terminator
				|| currentToken.type == CodeTokenizer.Token.Type.Pipe
			) {
				throw new UnexpectedTokenException("Expected statement, unexpected " + currentToken.ToString());
			} else if (currentToken.type == CodeTokenizer.Token.Type.Keyword) {
				if (currentToken.text == "IN") {
					// If inline, parse the inline statement and return it
					if (!isArgument) throw new UnexpectedTokenException("Expected statement start, unexpected " + currentToken.position.ToString());
					// Move next to the start of the inline statement
					if (!enumerator.MoveNext()) throw new EndOfFileException("Unexpected end of file, expected statement " + currentToken.position.ToString());
					return ParseStatement(ref enumerator, enviroment, false);
				} else {
					throw new FunctionNotFoundException("Keyword unknown " + currentToken.ToString());
				}
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
								ret = new SyntaxNodeTypes.Literal<float>(float.Parse(currentToken.text, CultureInfo.InvariantCulture), currentToken.position);
							} else throw new NumberLiteralException("Unknown number type '" + last + "' " + currentToken.position);
						} else {
							ret = new SyntaxNodeTypes.Literal<int>(int.Parse(currentToken.text), currentToken.position);
						}
					} catch (FormatException) {
						throw new NumberLiteralException("Number is not of correct format " + currentToken.position);
					}
				} else {
					statement = new SyntaxNodeTypes.Statement(currentToken.position);
					ret = statement;
				}
			}

			if (isArgument) {
				if (statement != null) statement.Validate(currentToken.text, enviroment);
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
						throw new EndOfFileException("Unexpected end of file, expected a argument, if statement end expected terminator " + lastPos);
					}
				}

				var current = enumerator.Current;
				if (current.type == CodeTokenizer.Token.Type.Terminator) {
					if (statement != null) statement.Validate(currentToken.text, enviroment);
					return ret;
				} else if (current.type == CodeTokenizer.Token.Type.Pipe) {
					if (statement != null) statement.Validate(currentToken.text, enviroment);
					var lastPos = enumerator.Current.position;
					if (!enumerator.MoveNext()) {
						throw new EndOfFileException("Unexpected end of file, expected a statement to pipe into " + lastPos);
					}
					return ParseStatement(ref enumerator, enviroment, false, ret);
				} else {
					if (statement != null) {
						statement.children.Add(ParseStatement(ref enumerator, enviroment, true));
					} else {
						throw new UnexpectedTokenException("Unexpected argument, expected a terminator " + current.position);
					}
				}
			}
		}

		public static void Parse(List<CodeTokenizer.Token> tokens, Enviroment enviroment) {
			IEnumerator<CodeTokenizer.Token> enumerator = tokens.GetEnumerator();

			while (enumerator.MoveNext()) {
				enviroment.AppendSyntaxNode(ParseStatement(ref enumerator, enviroment, false));
			}
		}

		public static void Parse(string text, Enviroment enviroment) => Parse(CodeTokenizer.Tokenize(text), enviroment);
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
			public Scope.Variable variable = null;

			public Statement(CodePosition position) {
				this.position = position;
			}

			public override object Evaluate() {
				if (function == null) {
					if (variable == null) throw new Exception("Statement not validated yet");
					else {
						throw new NotImplementedException("Evaluation not yet implemented");
					}
				} else {
					throw new NotImplementedException("Evaluation not yet implemented");
				}
			}

			public override Type GetReturnType() {
				return function?.returnType ?? variable?.Type ?? throw new Exception("Statement not validated yet");
			}

			public void Validate(string name, Enviroment enviroment) {
				if (name[0] == '&') {
					if (children.Count != 0) throw new FunctionNotFoundException("You cannot call variable query with any arguments " + position.ToString());
					variable = enviroment.GetVariable(name.Substring(1), position) ?? throw new FunctionNotFoundException("Failed to get variable " + name.Substring(1) + " " + position.ToString()); ;
				} else if (name[name.Length - 1] == '=') {
					if (children.Count != 1) throw new FunctionNotFoundException("Variable assignment must have one value " + position.ToString());
					variable = enviroment.GetVariable(name.Substring(0, name.Length - 1), position) ?? throw new FunctionNotFoundException("Failed to get variable " + name.Substring(0, name.Length - 1) + " " + position.ToString()); ;
					if (variable.Type != children[0].GetReturnType()) throw new FunctionNotFoundException("Cannot assign type " + enviroment.provider.GetTypeName(children[0].GetReturnType()) + " to variable of type " + enviroment.provider.GetTypeName(variable.Type) + " " + position.ToString());
				} else if (name.Length > 7 && name.Substring(0, 7) == "DEFINE:") {
					if (children.Count != 0) throw new FunctionNotFoundException("Variable definition cannot have arguments " + position.ToString());
					var segments = name.Split(':');
					if (segments.Length != 3) throw new FunctionNotFoundException("Variable definition in not in corret format, expected DEFINE:<name>:<type> " + position.ToString());
					var type = enviroment.provider.GetTypeByName(segments[2]);
					variable = enviroment.DefineVariable(segments[1], type, position);
				} else {
					if (function != null) throw new Exception("Tryied to validate a validated statement");
					var overloads = enviroment.provider.GetOverloads(name);
					var signature = enviroment.provider.GetFunctionSignature(name, children.Select(v => v.GetReturnType()));
					var signIndex = overloads.IndexOf(signature);
					if (signIndex == -1) {
						throw new FunctionNotFoundException("Function not found \"" + signature + "\"" + position.ToString() + ", posible overloads: {\n  " + string.Join(", \n  ", overloads) + "\n}");
					} else {
						function = enviroment.provider.GetFunction(signature);
					}
				}
			}

			public override string ToString() {
				return base.ToString() + "[" + children.Count + "]";
			}

			public override void GetDebug(Action<string> write, ref int indent) {
				if (function != null) {
					write("Statment[" + children.Count + "] = {");
					indent++;
					foreach (var child in children) {
						child.GetDebug(write, ref indent);
					}
					indent--;
					write("}");
				} else if (variable != null) {
					write("Variable " + (children.Count == 1 ? "assignment" : (variable.Position == position ? "definition" : "query")) + ": " + variable.GetType().FullName + " " + variable.Position);
				} else {
					throw new Exception("Statement not validated yet");
				}
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

	public class Scope {
		public class Variable {
			protected object value;
			protected Type type;
			protected CodePosition position;

			public object Value { get => value; }
			public CodePosition Position { get => position; }
			public Type Type { get => type; }

			public Variable(Type type, CodePosition position) {
				if (type.IsValueType) {
					value = Activator.CreateInstance(type);
				} else {
					value = null;
				}
				this.type = type;
				this.position = position;
			}

			public void SetValue(object newValue, CodePosition position, TypeInfoProvider provider) {
				if (newValue.GetType() == type) value = newValue;
				else throw new VariableException("Variable tryied to set a variable of type " + provider.GetTypeName(type) + " to type " + provider.GetTypeName(newValue.GetType()));
			}
		}

		public Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
		public CodePosition start;
		public int depth;
		public Scope parent;

		public Scope(Scope parent, CodePosition position) {
			start = position;
			this.parent = parent;
			depth = parent?.depth + 1 ?? 0;
		}

		public Variable GetVariable(string name) {
			if (variables.TryGetValue(name, out Variable ret)) {
				return ret;
			} else {
				return parent?.GetVariable(name);
			}
		}

		public Variable DefineVariable(string name, Type type, CodePosition position) {
			try {
				Variable ret = new Variable(type, position);
				variables.Add(name, ret);
				return ret;
			} catch (ArgumentException) {
				throw new VariableException("Variable named \"" + name + "\" is already defined " + position.ToString());
			}
		}

	}

	public class Enviroment {
		readonly public TypeInfoProvider provider = new TypeInfoProvider();
		protected Scope scope;
		protected List<SyntaxNode> nodes = new List<SyntaxNode>();

		public static Action<string> printFunction = (v) => { };

		protected Scope GetScope(CodePosition position) {
			return scope ?? (scope = new Scope(null, position));
		}
		public Scope.Variable GetVariable(string name, CodePosition position) {
			return GetScope(position).GetVariable(name);
		}

		public Scope.Variable DefineVariable(string name, Type type, CodePosition position) {
			return GetScope(position).DefineVariable(name, type, position);
		}
		public void SetVariableValue(string name, object value, CodePosition position) {
			GetScope(position).GetVariable(name).SetValue(value, position, provider);
		}

		public void AppendSyntaxNode(SyntaxNode node) => nodes.Add(node);

		public List<SyntaxNode> _DebugDumpNodes() => nodes;
	}
}
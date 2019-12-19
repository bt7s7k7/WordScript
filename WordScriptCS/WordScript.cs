using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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
	public sealed class TypeConversionAttribute : Attribute {
		public bool isStandard = false;
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public sealed class FunctionDefinitionAttribute : Attribute {
		public string name;
		public bool isStandard = false;

		public FunctionDefinitionAttribute(string name) {
			this.name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class WordScriptTypeAttribute : Attribute {

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
		private Dictionary<Type, string> names = new Dictionary<Type, string>();
		private Dictionary<string, Function> functions = new Dictionary<string, Function>();
		private Dictionary<string, List<string>> overloads = new Dictionary<string, List<string>>();
		private static TypeInfoProvider singleton = null;

		public static TypeInfoProvider GetGlobal() {
			if (singleton == null) {
				singleton = new TypeInfoProvider();
				singleton.LoadGlobals(false);
			}
			return singleton;
		}

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

		public TypeInfoProvider AddFunction(string name, Func<object[], object> func, Type returnType = null, IEnumerable<Type> arguments = null) {
			AddFunction(new Function {
				name = name,
				function = func,
				arguments = arguments ?? (new Type[] { }),
				returnType = returnType ?? typeof(void)
			});

			return this;
		}

		public string GetFunctionSignature(string name, IEnumerable<Type> arguments) {
			return name + (arguments.Count() > 0 ? " " + string.Join(" ", arguments.Select(v => GetTypeName(v))) : "");
		}

		public string GetTypeName(Type type) {
			if (type.IsGenericType && !type.IsGenericTypeDefinition) {
				return GetTypeName(type.GetGenericTypeDefinition()) + "!" + string.Join("!", type.GetGenericArguments().Select(v => GetTypeName(v)));
			} else if (names.TryGetValue(type, out string name)) {
				return name;
			} else {
				throw new TypeNotRegisteredException("Type " + type.FullName + " is not registered");
			}
		}

		/// <summary>
		/// Finds a function with the signature. If no found throw <see cref="FunctionNotFoundException"></see>
		/// </summary>
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

		/// <summary>
		/// Loads types and functions specified by attributes, if you want all functions use: <see cref="TypeInfoProvider.GetGlobal"/>. Only use to include standard functions such as "add" or "string"
		/// </summary>
		public TypeInfoProvider LoadGlobals(bool standardOnly = true) {
			names = new Dictionary<Type, string>();
			functions = new Dictionary<string, Function>();
			overloads = new Dictionary<string, List<string>>();
			var types = AppDomain.CurrentDomain.GetAssemblies().AsParallel()
				.SelectMany((v) => v.GetTypes());
			var methods = types
				.SelectMany(v => v.GetMethods())
				.Where(v => v.IsStatic && !v.IsGenericMethodDefinition && !v.IsGenericMethod);

			foreach (var method in methods) {
				var nameAttr = method.GetCustomAttribute<TypeNameAttribute>(false);
				if (nameAttr != null) {
					var name = (string)method.Invoke(null, new object[] { });

					names.Add(nameAttr.targetType, name);
				}
			}

			foreach (var name in names) {
				AddFunction("block!" + name.Value + ".invoke", (args) => {
					var block = args[0];
					return block.GetType().GetMethod("Evaluate").Invoke(block, new object[] { });
				}, name.Key, new Type[] { typeof(TypedStatementBlock<>).MakeGenericType(name.Key) });
			}

			foreach (var method in methods) {
				var convAttr = method.GetCustomAttribute<TypeConversionAttribute>(false);
				if (convAttr != null) {
					if (standardOnly && !convAttr.isStandard) continue;
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
					if (standardOnly && !funcDefAttr.isStandard) continue;
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
			var listMethod = typeof(TypeInfoProvider).GetMethod("CreateArrayFunctions", BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var name in names) {
				var funcName = "array!" + name.Value;
				var elemType = name.Key;

				if (elemType.IsGenericTypeDefinition) continue;

				listMethod.MakeGenericMethod(elemType).Invoke(this, new object[] { funcName });
			}

			if (!standardOnly) {
				foreach (var type in types) {
					var typeAttr = type.GetCustomAttribute<WordScriptTypeAttribute>();
					if (typeAttr != null) {
						MapType(type);
					}
				}
			}
			return this;
		}

		public Type GetTypeByName(string name) {
			var retCan = names.Where(v => v.Value == name).Select(v => v.Key);
			if (retCan.Count() < 1) {
				if (name.Contains('!')) {
					var ss = name.Split('!');
					var generic = GetTypeByName(ss.First());
					var arguments = ss.Skip(1).Select(v => GetTypeByName(v));
					return generic.MakeGenericType(arguments.ToArray());
				} else throw new TypeNotRegisteredException("There is no type registered for name " + name);
			} else return retCan.First();
		}

		/// <summary>
		/// Includes the functions from a source provider, defaults to global provider. Add all function overloads, select by name
		/// </summary>
		public TypeInfoProvider Include(IEnumerable<string> functionNames, TypeInfoProvider source = null) {
			source = source ?? TypeInfoProvider.GetGlobal();

			names = new Dictionary<Type, string>(source.names);

			foreach (var name in functionNames) {
				foreach (var signature in source.GetOverloads(name)) {
					AddFunction(source.GetFunction(signature));
				}
			}

			return this;

		}

		public void MapType(Type type) {
			string typeName = type.Name;
			if (!names.ContainsKey(type)) {
				names.Add(type, typeName);
			}

			AddFunction("string", (v) => v[0].ToString(), typeof(string), new Type[] { type });

			void regiserMethod(MethodInfo method) {
				if (!names.ContainsKey(method.ReturnType)) names.Add(method.ReturnType, method.ReturnType.Name);
				foreach (var argument in method.GetParameters()) {
					if (!names.ContainsKey(argument.ParameterType)) names.Add(argument.ParameterType, argument.ParameterType.Name);
				}

				try {
					if (method.IsStatic) {
						AddFunction(typeName + "." + method.Name, (v) => method.Invoke(null, v), method.ReturnType, method.GetParameters().Select(v => v.ParameterType));
					} else {
						AddFunction(typeName + "." + method.Name, (v) => method.Invoke(v[0], v.Skip(1).ToArray()), method.ReturnType, new Type[] { type }.Concat(method.GetParameters().Select(v => v.ParameterType)));
					}
				} catch (ArgumentException) {

				}
			};

			foreach (var method in type.GetMethods()) {
				regiserMethod(method);
			}

			foreach (var field in type.GetFields()) {
				if (!names.ContainsKey(field.FieldType)) names.Add(field.FieldType, field.FieldType.Name);
				if (field.IsStatic) {
					AddFunction(typeName + "." + field.Name, (v) => field.GetValue(null), field.FieldType, null);
					AddFunction(typeName + "." + field.Name, (v) => {
						field.SetValue(null, v[0]);
						return field.GetValue(null);
					}, field.FieldType, new Type[] { field.FieldType });
				} else {
					AddFunction(typeName + "." + field.Name, (v) => field.GetValue(v[0]), field.FieldType, new Type[] { type });
					AddFunction(typeName + "." + field.Name, (v) => {
						field.SetValue(v[0], v[1]);
						return field.GetValue(null);
					}, field.FieldType, new Type[] { type, field.FieldType });
				}
			}

			foreach (var constructor in type.GetConstructors()) {
				AddFunction(typeName, (v) => Activator.CreateInstance(type, v), type, constructor.GetParameters().Select(v => v.ParameterType));
			}

			foreach (var property in type.GetProperties()) {
				if (property.GetMethod != null) regiserMethod(property.GetMethod);
				if (property.SetMethod != null) regiserMethod(property.SetMethod);
			}
		}

		private void CreateArrayFunctions<T>(string funcName) {
			var elemType = typeof(T);
			var listType = typeof(List<>).MakeGenericType(elemType);
			AddFunction(funcName, (v) => new List<T>(), listType);

			funcName += ".";

			AddFunction(funcName + "at", (v) => ((List<T>)v[0])[(int)v[1]], elemType, new Type[] { listType, typeof(int) });
			AddFunction(funcName + "size", (v) => ((List<T>)v[0]).Count, typeof(int), new Type[] { listType });
			AddFunction(funcName + "push", (v) => { ((List<T>)v[0]).Add((T)v[1]); return v[0]; }, listType, new Type[] { listType, elemType });
			AddFunction(funcName + "forEach", (v) => {
				((List<T>)v[0]).ForEach(w => {
					((TypedVariable<T>)v[1]).Value = w;
					((VoidBlock)v[2]).Evaluate();
				});
				return v[0];
			}, listType, new Type[] { listType, typeof(TypedVariable<T>), typeof(VoidBlock) });

		}
	}

	public static class DefaultTypeInfo {

		[TypeName(typeof(TypedVariable<>))]
		public static string GetTypedVariableName() => "variable";

		[TypeName(typeof(TypedStatementBlock<>))]
		public static string GetTypedStatementBlockName() => "block";

		[TypeName(typeof(List<>))]
		public static string GetEnumerableName() => "array";

		[TypeName(typeof(string))]
		public static string GetStringName() => "string";

		[TypeName(typeof(int))]
		public static string GetIntName() => "int";

		[TypeName(typeof(float))]
		public static string GetFloatName() => "float";

		[TypeName(typeof(FlowControllWrapper<>))]
		public static string GetFCWName() => "fcw";

		[TypeName(typeof(bool))]
		public static string GetBoolName() => "bool";

		[TypeConversion(isStandard = true)]
		public static int FloatToInt(float v) => (int)Math.Floor(v);

		[TypeConversion(isStandard = true)]
		public static float IntToFloat(int v) => v;

		[TypeConversion(isStandard = true)]
		public static int StringToInt(string v) => int.Parse(v);

		[TypeConversion(isStandard = true)]
		public static float StringToFloat(string v) => float.Parse(v, CultureInfo.InvariantCulture);

		[FunctionDefinition("add", isStandard = true)]
		public static string AddString(string a, string b) => a + b;

		[FunctionDefinition("add", isStandard = true)]
		public static int AddInt(int a, int b) => a + b;

		[FunctionDefinition("add", isStandard = true)]
		public static float AddFloat(float a, float b) => a + b;

		[FunctionDefinition("sub", isStandard = true)]
		public static int SubInt(int a, int b) => a - b;

		[FunctionDefinition("sub", isStandard = true)]
		public static float SubFloat(float a, float b) => a - b;

		[FunctionDefinition("mul", isStandard = true)]
		public static int MulInt(int a, int b) => a * b;

		[FunctionDefinition("mul", isStandard = true)]
		public static float MulFloat(float a, float b) => a * b;

		[FunctionDefinition("div", isStandard = true)]
		public static int DivInt(int a, int b) => a / b;

		[FunctionDefinition("div", isStandard = true)]
		public static float DivFloat(float a, float b) => a / b;

		[FunctionDefinition("not", isStandard = true)]
		public static bool Not(bool a) => !a;

		[FunctionDefinition("and", isStandard = true)]
		public static bool And(bool a, bool b) => a && b;

		[FunctionDefinition("or", isStandard = true)]
		public static bool Or(bool a, bool b) => a || b;

		[FunctionDefinition("gt", isStandard = true)]
		public static bool Greather(int a, int b) => a > b;

		[FunctionDefinition("ls", isStandard = true)]
		public static bool Lesser(int a, int b) => a < b;

		[FunctionDefinition("gte", isStandard = true)]
		public static bool GreatherOrEqual(int a, int b) => a >= b;

		[FunctionDefinition("lse", isStandard = true)]
		public static bool LesserOrEqual(int a, int b) => a <= b;

		[FunctionDefinition("gt", isStandard = true)]
		public static bool Greather(float a, float b) => a > b;

		[FunctionDefinition("ls", isStandard = true)]
		public static bool Lesser(float a, float b) => a < b;

		[FunctionDefinition("gte", isStandard = true)]
		public static bool GreatherOrEqual(float a, float b) => a >= b;

		[FunctionDefinition("lse", isStandard = true)]
		public static bool LesserOrEqual(float a, float b) => a <= b;

		[FunctionDefinition("true", isStandard = true)]
		public static bool True() => true;

		[FunctionDefinition("false", isStandard = true)]
		public static bool False() => false;

		[FunctionDefinition("if", isStandard = true)]
		public static bool If(bool condition, VoidBlock action) {
			if (condition) {
				action.Evaluate();
			}

			return !condition;
		}

		[FunctionDefinition("if", isStandard = true)]
		public static bool If(bool condition, VoidBlock thenAction, VoidBlock elseAction) {
			if (condition) {
				thenAction.Evaluate();
			} else {
				elseAction.Evaluate();
			}
			return !condition;
		}
		
		[FunctionDefinition("if", isStandard = true)]
		public static bool If(bool prev, bool condition, VoidBlock thenAction, VoidBlock elseAction) {
			if (prev && condition) {
				thenAction.Evaluate();
			} else {
				elseAction.Evaluate();
			}

			return !(prev && condition);
		}
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
						if (code[position] == '\n') codePosition.NextLine();

						if (isEscape) {
							if (code[position] == 'n') stringBuilder.Append("\n");
							else if (code[position] == '\\') stringBuilder.Append("\\");
							else if (code[position] == 'b') stringBuilder.Append("\b");
							else if (code[position] == 'r') stringBuilder.Append("\r");
							else if (code[position] == '\'') stringBuilder.Append("'");
							else if (code[position] == '"') stringBuilder.Append("\"");
							else throw new UnknownEscapeCharacterException("Unknown escape character \\" + code[position] + " " + codePosition.ToString());

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
				} else if (isString) {
					ret.Add(Token.MakeStringLiteral(word, codePosition));
				} else if (word.Where(v => char.IsLetter(v) && char.ToUpper(v) == v).Count() == word.Length) {
					ret.Add(Token.MakeKeyword(word, codePosition));
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
				} else if (currentToken.text == "BLOCK" || currentToken.text == "ACTION") {
					var block = Parse(ref enumerator, enviroment, currentToken.position, false);
					block.Validate(enviroment);
					object value = currentToken.text == "ACTION" ? new VoidBlock { block = block } : block.CreateTypedBlock();

					ret = (SyntaxNode)Activator.CreateInstance(typeof(SyntaxNodeTypes.Literal<>).MakeGenericType(value.GetType()), new object[] { value, currentToken.position });
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
								ret = new SyntaxNodeTypes.Literal<int>(int.Parse(currentToken.text.Substring(0, currentToken.text.Length - 1)), currentToken.position);
							} else if (last == 'f') {
								ret = new SyntaxNodeTypes.Literal<float>(float.Parse(currentToken.text.Substring(0, currentToken.text.Length - 1), CultureInfo.InvariantCulture), currentToken.position);
							} else throw new NumberLiteralException("Unknown number type '" + last + "' " + currentToken.position);
						} else {
							ret = new SyntaxNodeTypes.Literal<int>(int.Parse(currentToken.text), currentToken.position);
						}
					} catch (FormatException) {
						throw new NumberLiteralException("Number is not of correct format /* Check number type suffix */ " + currentToken.position);
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
						throw new EndOfFileException("Unexpected end of file, expected a argument, terminator or pipe " + lastPos);
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
						throw new UnexpectedTokenException("Unexpected argument, expected a terminator  /* Only statements can have arguments */ " + current.position);
					}
				}
			}
		}

		public static StatementBlock Parse(ref IEnumerator<CodeTokenizer.Token> enumerator, Enviroment enviroment, CodePosition position, bool inline = false) {

			if (!inline) enviroment.StartBlock(position);

			while (enumerator.MoveNext()) {
				if (enumerator.Current.type == CodeTokenizer.Token.Type.Keyword && enumerator.Current.text == "END") {
					break;
				}
				enviroment.AppendSyntaxNode(ParseStatement(ref enumerator, enviroment, false));
			}

			return inline ? null : enviroment.EndBlock();
		}

		public static StatementBlock Parse(string text, Enviroment enviroment, CodePosition position, bool inline = false) {
			var tokens = (IEnumerator<CodeTokenizer.Token>)CodeTokenizer.Tokenize(text).GetEnumerator();
			return Parse(ref tokens, enviroment, position, inline);
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

	public enum FlowControllType {
		None,
		Return
	}

	public struct FlowControllWrapper<T> {

		public FlowControllType type;
		public T value;

		public FlowControllWrapper(FlowControllType type, T value) {
			this.type = type;
			this.value = value;
		}
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
						if (children.Count == 1) {
							variable.SetValue(children[0].Evaluate());
							return variable.Value;
						} else if (variable.Position == position) {
							// We are a declaration, so all the hard work has been done aleready at compile time
							return variable.Value;
						} else {
							return variable.Value;
						}
					}
				} else {
					return function.function(children.Select(v => v.Evaluate()).ToArray());
				}
			}

			public override Type GetReturnType() {
				return function?.returnType ?? variable?.Type ?? throw new Exception("Statement not validated yet");
			}

			public void Validate(string name, Enviroment enviroment, bool noImplicitConversion = false) {
				if (name == "return") {
					if (children.Count != 1) throw new FunctionNotFoundException("Return statement must have one argument " + position.ToString());

					Type childType = children[0].GetReturnType();
					Type returnType = typeof(FlowControllWrapper<>).MakeGenericType(children[0].GetReturnType());
					function = new Function {
						arguments = new Type[] { childType },
						function = (v) => Activator.CreateInstance(returnType, new object[] { FlowControllType.Return, v[0] }),
						name = name,
						returnType = returnType
					};
				} else if (name == "string") {
					function = new Function {
						arguments = children.Select(v => v.GetReturnType()),
						returnType = typeof(string),
						name = "string",
						function = (v) => string.Join(" ", v.Select(w => w.ToString()))
					};
				} else if (name == "eq") {
					if (children.Count != 2) throw new FunctionNotFoundException("Equals statement must have 2 arguments " + position.ToString());
					var aType = children[0].GetReturnType();
					var bType = children[1].GetReturnType();
					function = new Function {
						arguments = new Type[] { aType, bType },
						returnType = typeof(bool),
						name = "eq",
						function = (v) => Object.Equals(v[0], v[1])
					};
				} else if (name[0] == '&') {
					if (name.Length > 1 && name[1] == '&') {
						if (children.Count != 0) throw new FunctionNotFoundException("You cannot call variable reference query with any arguments " + position.ToString());
						var variable = enviroment.GetVariable(name.Substring(2)) ?? throw new FunctionNotFoundException("Failed to get variable " + name.Substring(1) + " " + position.ToString());
						Type typedVariableType = typeof(TypedVariable<>).MakeGenericType(variable.Type);
						var typedVariable = Activator.CreateInstance(typedVariableType, new object[] { variable });
						function = new Function {
							name = name,
							arguments = new Type[0],
							returnType = typedVariableType,
							function = (v) => typedVariable
						};
					} else {
						if (children.Count != 0) throw new FunctionNotFoundException("You cannot call variable query with any arguments " + position.ToString());
						variable = enviroment.GetVariable(name.Substring(1)) ?? throw new FunctionNotFoundException("Failed to get variable " + name.Substring(1) + " " + position.ToString()); ;
					}
				} else if (name[name.Length - 1] == '=') {
					if (children.Count != 1) throw new FunctionNotFoundException("Variable assignment must have one value " + position.ToString());
					variable = enviroment.GetVariable(name.Substring(0, name.Length - 1)) ?? throw new FunctionNotFoundException("Failed to get variable " + name.Substring(0, name.Length - 1) + " " + position.ToString()); ;
					if (variable.Type != children[0].GetReturnType()) throw new FunctionNotFoundException("Cannot assign type " + enviroment.provider.GetTypeName(children[0].GetReturnType()) + " to variable of type " + enviroment.provider.GetTypeName(variable.Type) + " " + position.ToString());
				} else if (name.Length > 7 && name.Substring(0, 7) == "DEFINE:") {
					var segments = name.Split(':');
					if (segments.Length == 3) {
						if (children.Count != 0) throw new FunctionNotFoundException("Variable definition cannot have arguments, expected DEFINE:<name>:<type> " + position.ToString());
						var type = enviroment.provider.GetTypeByName(segments[2]);

						var variable = enviroment.DefineVariable(segments[1][0] == '&' ? segments[1].Substring(1) : segments[1], type, position);
						var returnType = variable.Type;
						var isRef = false;
						if (segments[1][0] == '&') {
							returnType = typeof(TypedVariable<>).MakeGenericType(returnType);
							segments[1] = segments[1].Substring(1);
							isRef = true;
						}

						if (isRef) {
							function = new Function {
								arguments = new Type[] { },
								function = (v) => {
									return Activator.CreateInstance(returnType, new object[] { variable });
								},
								name = name,
								returnType = returnType
							};
						} else {
							this.variable = variable;
						}
					} else if (segments.Length == 2) {
						if (children.Count != 1) throw new FunctionNotFoundException("Variable definition must have 1 argument, expected DEFINE:<name> <value> " + position.ToString());
						var type = children[0].GetReturnType();
						var variable = enviroment.DefineVariable(segments[1][0] == '&' ? segments[1].Substring(1) : segments[1], type, position);
						var returnType = variable.Type;
						var isRef = false;
						if (segments[1][0] == '&') {
							returnType = typeof(TypedVariable<>).MakeGenericType(returnType);
							segments[1] = segments[1].Substring(1);
							isRef = true;
						}
						function = new Function {
							arguments = new Type[] { type },
							function = (v) => {
								variable.SetValue(v[0]);
								if (isRef) return Activator.CreateInstance(returnType, new object[] { variable });
								else return variable.Value;
							},
							name = name,
							returnType = returnType
						};
					} else throw new FunctionNotFoundException("Variable definition in not in correct format, expected DEFINE:<name>:<type> or DEFINE:<name> <value>" + position.ToString());
				} else {

					if (name[0] == '.' && children.Count > 0) {
						name = enviroment.provider.GetTypeName(children[0].GetReturnType()) + name;
					}

					if (function != null) throw new Exception("Tryied to validate a validated statement");
					var signature = enviroment.provider.GetFunctionSignature(name, children.Select(v => v.GetReturnType())).Split(' ');
					var overloads = enviroment.provider.GetOverloads(name).Select(v => v.Split(' ')).Where(v => v.Length == signature.Length);
					var weights = overloads.Select(overload => {
						int weight = 0;
						Function[] _convertors = new Function[overload.Length - 1];
						for (int i = 1, len = overload.Length; i < len; i++) {
							var overloadType = overload[i];
							var signatureType = signature[i];

							if (overloadType == signatureType) {
								// The same so don't add weight
								_convertors[i - 1] = null; // No conversion needed
							} else {
								if (overloadType == "string") {
									_convertors[i - 1] = new Function {
										arguments = new Type[] { enviroment.provider.GetTypeByName(signatureType) },
										returnType = typeof(string),
										name = "string",
										function = (v) => v[0].ToString()
									};
								} else try {
										_convertors[i - 1] = enviroment.provider.GetFunction(overloadType + " " + signatureType);
									} catch (FunctionNotFoundException) {
										return (overload, convertors: new Function[0], weight: -1);
									}

								weight++;
							}
						}
						return (overload, convertors: _convertors, weight);
					}).Where(v => v.weight >= 0);

					if (noImplicitConversion) weights = weights.Where(v => v.weight == 0);

					if (!weights.Any()) throw new FunctionNotFoundException("Function not found \"" + string.Join(" ", signature) + "\"" + position.ToString() + ", posible overloads: {\n  " + string.Join(", \n  ", overloads) + "\n}");

					(string[] overload, Function[] convertors, int weight) toUse = weights.First();

					for (int i = 0, len = weights.Count(); i < len; i++) {
						var curr = weights.ElementAt(i);
						if (curr.weight < toUse.weight) toUse = curr;
					}

					var overloadTypes = toUse.overload.Skip(1);
					var convertors = toUse.convertors;

					for (int i = 0, len = overloadTypes.Count(); i < len; i++) {
						if (convertors[i] != null) {
							var child = children[i];
							var wrapper = new Statement(child.position);
							wrapper.children.Add(child);
							wrapper.Validate(overloadTypes.ElementAt(i), enviroment, true);

							children[i] = wrapper;
						}
					}

					function = enviroment.provider.GetFunction(string.Join(" ", toUse.overload));
				}
			}

			public override string ToString() {
				return base.ToString() + "[" + children.Count + "]";
			}

			public override void GetDebug(Action<string> write, ref int indent) {
				if (function != null) {
					write("Statment[" + children.Count + "] \"" + function.name + " " + string.Join(" ", function.arguments.Select(v => v.Name)) + "\":" + function.returnType.Name + " = {");
					indent++;
					foreach (var child in children) {
						child.GetDebug(write, ref indent);
					}
					indent--;
					write("}");
				} else if (variable != null) {
					write("Variable " + (children.Count == 1 ? "assignment" : (variable.Position == position ? "definition" : "query")) + ": " + variable.Type + " " + variable.Position);
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
	public class TypedVariable<T> {
		protected Scope.Variable variable;

		public TypedVariable(Scope.Variable variable) {
			this.variable = variable;
		}

		public T Value { get => (T)variable.Value; set => variable.SetValue(value); }
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

			public void SetValue(object newValue) {
				if (newValue.GetType() == type) value = newValue;
				else throw new Exception("Variable tryied to set a variable of type " + type.FullName + " to type " + newValue.GetType().FullName);
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

	public class TypedStatementBlock<T> {
		public StatementBlock block;
		public T Evaluate() {
			return (T)block.Evaluate();
		}

		public TypedStatementBlock(StatementBlock block) {
			this.block = block;
		}

		public override string ToString() {
			return "[block[" + block.GetSyntaxNodes().Count + "]]:" + typeof(T).Name;
		}
	}

	public class VoidBlock {
		public StatementBlock block;

		public void Evaluate() {
			block.Evaluate();
		}

		[FunctionDefinition("action.invoke")]
		public static void Evaluate(VoidBlock voidBlock) => voidBlock.block.Evaluate();

		[TypeName(typeof(VoidBlock))]
		public static string GetTypeName() => "action";

		public override string ToString() {
			return "[action[" + block.GetSyntaxNodes().Count + "]]";
		}
	}

	public class StatementBlock {
		protected List<SyntaxNode> nodes = new List<SyntaxNode>();
		protected Scope scope;
		public readonly CodePosition position;
		protected Type retType = null;
		public Scope Scope => scope;
		public Type ReturnType => retType;

		public void AppendSyntaxNode(SyntaxNode node) => nodes.Add(node);

		public StatementBlock(Scope scope, CodePosition position) {
			this.scope = new Scope(scope, position);
			this.position = position;
		}

		public void Validate(Enviroment enviroment) {
			if (nodes.Count == 1 && nodes[0].GetReturnType().IsGenericType && nodes[0].GetReturnType().GetGenericTypeDefinition() != typeof(FlowControllWrapper<>)) {
				retType = nodes[0].GetReturnType();
				return;
			}

			foreach (var node in nodes) {
				var returnType = node.GetReturnType();
				if (returnType.IsConstructedGenericType && returnType.GetGenericTypeDefinition() == typeof(FlowControllWrapper<>)) {
					if (retType == null) retType = returnType.GetGenericArguments()[0];
					if (returnType.GetGenericArguments()[0] != retType) throw new ReturnTypeException("Return statement is not of type " + enviroment.provider.GetTypeName(retType) + " /* All return statements must be of same type, dictated by the first one */ " + node.position.ToString());
				}
			}

			if (retType == null) retType = typeof(void);
		}

		public object Evaluate() {
			if (nodes.Count == 1) {
				object res = nodes[0].Evaluate();
				if (res == null) return null;
				var resType = res.GetType();
				if (resType.IsGenericType && resType.GetGenericTypeDefinition() == typeof(FlowControllWrapper<>)) {
					res = resType.GetField("value").GetValue(res);
				}
				return res;
			}

			foreach (var node in nodes) {
				var res = node.Evaluate();
				if (res == null) continue;
				var resType = res.GetType();
				if (resType.IsGenericType && resType.GetGenericTypeDefinition() == typeof(FlowControllWrapper<>)) {
					return resType.GetField("value").GetValue(res);
				}
			}

			return null;
		}

		public List<SyntaxNode> GetSyntaxNodes() => nodes;

		public object CreateTypedBlock() {
			if (retType == null) throw new Exception("Block has not been validated yet");
			else {
				if (retType == typeof(void)) {
					return new VoidBlock { block = this };
				} else {
					var type = typeof(TypedStatementBlock<>).MakeGenericType(new Type[] { retType });
					return Activator.CreateInstance(type, new object[] { this });
				}
			}
		}
	}

	[Serializable]
	public class ReturnTypeException : WordScriptException {
		public ReturnTypeException() {
		}

		public ReturnTypeException(string message) : base(message) {
		}

		public ReturnTypeException(string message, Exception innerException) : base(message, innerException) {
		}

		protected ReturnTypeException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}

	public class Enviroment {
		readonly public TypeInfoProvider provider;
		protected Scope scope = new Scope(null, new CodePosition("-root-"));
		protected Stack<StatementBlock> blocks = new Stack<StatementBlock>();

		public Enviroment(TypeInfoProvider provider) {
			this.provider = provider;
		}

		protected Scope GetScope() {
			return GetBlock().Scope;
		}

		public StatementBlock GetBlock() {
			return blocks.FirstOrDefault() ?? throw new BlockException("No blocks have been stated yet");
		}

		public Scope.Variable GetVariable(string name) {
			return GetScope().GetVariable(name);
		}

		public Scope.Variable DefineVariable(string name, Type type, CodePosition position) {
			return GetScope().DefineVariable(name, type, position);
		}
		public void SetVariableValue(string name, object value) {
			GetScope().GetVariable(name).SetValue(value);
		}

		public void StartBlock(CodePosition position) {
			blocks.Push(new StatementBlock(blocks.FirstOrDefault()?.Scope ?? scope, position));
		}

		public StatementBlock EndBlock() {
			try {
				return blocks.Pop();
			} catch (InvalidOperationException) {
				throw new BlockException("No blocks have been started yet");
			}
		}

		public void AppendSyntaxNode(SyntaxNode node) => GetBlock().AppendSyntaxNode(node);

	}

	[Serializable]
	public class BlockException : WordScriptException {
		public BlockException() {
		}

		public BlockException(string message) : base(message) {
		}

		public BlockException(string message, Exception innerException) : base(message, innerException) {
		}

		protected BlockException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
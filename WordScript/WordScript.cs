using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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


}
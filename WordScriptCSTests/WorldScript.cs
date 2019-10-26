using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WordScript;

namespace WordScript.Tests {

	/// <summary>
	/// Summary description for WorldScript
	/// </summary>
	[TestClass]
	public class WorldScript {

		public WorldScript() {
			//
			// TODO: Add constructor logic here
			//
		}

		[AssemblyInitialize]
		public static void Setup(TestContext context) {
			TypeInfoProvider.GetGlobal().AddFunction("print", (a) => {
				context.WriteLine((string)a[0]);
				return null;
			}, null, new Type[] { typeof(string) });
		}

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext {
			get {
				return testContextInstance;
			}
			set {
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion
		TypeInfoProvider provider = TypeInfoProvider.GetGlobal();

		[TestMethod]
		public void GettingDefaultTypeName() {
			Assert.AreEqual(provider.GetTypeName(typeof(string)), "string");
		}

		public class TestClass {
			public int testValue;

			[TypeName(typeof(TestClass))]
			public static string GetTypeName() => "TestClass";

			[TypeConversion]
			public static int ToInt(TestClass inp) => inp.testValue;

			[TypeConversion]
			public static TestClass FromInt(int inp) => new TestClass { testValue = inp };

			[TypeName(typeof(TestGenericClass<>))]
			public static string TestGenericName() => "TestGenericClass";
			[TypeConversion]
			public static TestGenericClass<int> TestGenericFromInt(int i) => new TestGenericClass<int>();
		}

		[TestMethod]
		public void GettingCustomTypeName() {
			Assert.AreEqual(provider.GetTypeName(typeof(TestClass)), TestClass.GetTypeName());
		}

		[TestMethod]
		public void CustomConversion() {
			var overloads = provider.GetOverloads("TestClass");
			Assert.AreEqual(overloads.Count, 1, "Function signature not found, error in GetOverloads or [TypeConversion]");
			var function = provider.GetFunction(overloads[0]);
			Assert.AreEqual(function.returnType, typeof(TestClass), "Function return value is not correct. Either a different overload is registered or error");
		}

		[TestMethod]
		public void Tokenizing() {
			var tokens = CodeTokenizer.Tokenize("print IN add \"Hello\" \"world\" . .\n  mul 5 10 , string , add \" = 25\" , print .");
			Assert.AreEqual(tokens.Count, 18);
		}

		[TestMethod]
		public void Parsing() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			var block = TokenParser.Parse("\"Comment\" .\nprint IN string 25 . .\nprint IN add \"Hello\" \"world\" . .\n  mul 5 10 , string , add \" = 25\" , print .", enviroment, CodePosition.GetExternal());
			var statements = block.GetSyntaxNodes();
			Assert.AreEqual(statements.Count, 4);
		}

		[TestMethod]
		public void VariableDefinition() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.StartBlock(CodePosition.GetExternal());
			Assert.IsNull(enviroment.GetVariable("y"));
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
			Assert.IsNotNull(enviroment.GetVariable("x"));
			enviroment.EndBlock();
		}

		[TestMethod]
		[ExpectedException(typeof(VariableException))]
		public void VariableRedefinitionFail() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.StartBlock(CodePosition.GetExternal());
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
			enviroment.EndBlock();
		}

		[TestMethod]
		public void VariablesInCode() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.StartBlock(CodePosition.GetExternal());
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
			TokenParser.Parse("DEFINE:y:int . &y . y= 0 . &x . x= &y .", enviroment, CodePosition.GetExternal(), true);

			Assert.IsNotNull(enviroment.GetVariable("y"));
			Assert.AreEqual(enviroment.GetVariable("y").Type, typeof(int));
			enviroment.EndBlock();
		}

		[TestMethod]
		[ExpectedException(typeof(FunctionNotFoundException))]
		public void VariableTypeSafetyInCode() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.StartBlock(CodePosition.GetExternal());
			enviroment.DefineVariable("x", typeof(string), CodePosition.GetExternal());
			TokenParser.Parse("x= 0 .", enviroment, CodePosition.GetExternal());
			enviroment.EndBlock();
		}

		[TestMethod]
		public void VariableInheritance() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.StartBlock(CodePosition.GetExternal());
			var xOut = enviroment.DefineVariable("x", typeof(string), CodePosition.GetExternal());
			var yOut = enviroment.DefineVariable("y", typeof(int), CodePosition.GetExternal());
			enviroment.StartBlock(CodePosition.GetExternal());
			Assert.AreEqual(yOut, enviroment.GetVariable("y"));
			Assert.AreEqual(xOut, enviroment.GetVariable("x"));
			var yInner = enviroment.DefineVariable("y", typeof(string), CodePosition.GetExternal());
			var zInner = enviroment.DefineVariable("z", typeof(string), CodePosition.GetExternal());
			Assert.AreNotEqual(yOut, enviroment.GetVariable("y"));
			enviroment.EndBlock();
			Assert.AreEqual(yOut, enviroment.GetVariable("y"));
			Assert.IsNull(enviroment.GetVariable("z"));

			enviroment.EndBlock();
		}

		[TestMethod]
		public void StandardInclusion() {
			TypeInfoProvider provider = new TypeInfoProvider().LoadGlobals();

			Assert.IsNotNull(provider.GetFunction("int float"));
		}

		[TestMethod]
		[ExpectedException(typeof(FunctionNotFoundException))]
		public void StandardInclusionFiltering() {
			TypeInfoProvider provider = new TypeInfoProvider().LoadGlobals();

			Assert.IsNotNull(provider.GetFunction("TestMethod int"));
		}

		[TestMethod()]
		public void AddFunctionTest() {
			TypeInfoProvider provider = new TypeInfoProvider();

			provider.AddFunction("test", (v) => v, typeof(void), new Type[] { });

			Assert.IsNotNull(provider.GetFunction("test"));
		}

		[TestMethod()]
		public void GetFunctionSignatureTest() {
			Assert.AreEqual(provider.GetFunctionSignature("test", new Type[] { }), "test");
			Assert.AreEqual(provider.GetFunctionSignature("testa", new Type[] { }), "testa");
			Assert.AreEqual(provider.GetFunctionSignature("testb", new Type[] { typeof(int) }), "testb int");
		}

		[TestMethod()]
		public void StatementBlockPosition() {
			Enviroment enviroment = new Enviroment(provider);
			var block = TokenParser.Parse("int 0f .", enviroment, CodePosition.GetExternal());
			Assert.AreEqual(block.position, CodePosition.GetExternal());
		}

		public class TestGenericClass<T> {
			

		}

		[TestMethod]
		public void GenericTypeNames() {
			Assert.AreEqual(provider.GetTypeName(typeof(TestGenericClass<int>)), "TestGenericClass!int");
		}

		[TestMethod]
		public void GenericTypesFromName() {
			Assert.AreEqual(provider.GetTypeByName("TestGenericClass!string"), typeof(TestGenericClass<string>));
		}

		[TestMethod]
		public void ReturningFromBlock() {
			Enviroment enviroment = new Enviroment(provider);
			var block = TokenParser.Parse("return 10 .", enviroment, CodePosition.GetExternal());
			block.Validate(enviroment);
			Assert.AreEqual(block.ReturnType, typeof(int));
		}
		
		[TestMethod]
		public void ReturningFromBlockMultiple() {
			Enviroment enviroment = new Enviroment(provider);
			var block = TokenParser.Parse("return 10 . return 15 .", enviroment, CodePosition.GetExternal());
			block.Validate(enviroment);
			Assert.AreEqual(block.ReturnType, typeof(int));
		}
		
		[TestMethod]
		[ExpectedException(typeof(ReturnTypeException))]
		public void ReturningFromBlockMismatchDetect() {
			Enviroment enviroment = new Enviroment(provider);
			var block = TokenParser.Parse("return 10 . return 1f .", enviroment, CodePosition.GetExternal());
			block.Validate(enviroment);
			Assert.AreEqual(block.ReturnType, typeof(int));
		}

		[TestMethod]
		public void CodeExecution() {
			Enviroment enviroment = new Enviroment(provider);
			Assert.AreEqual(TokenParser.Parse("add 10 15 .", enviroment, CodePosition.GetExternal()).Evaluate(), 25);
		}

		[TestMethod]
		public void CodeExecutionReturn() {
			Enviroment enviroment = new Enviroment(provider);
			Assert.AreEqual(TokenParser.Parse("add 10 15 , return .", enviroment, CodePosition.GetExternal()).Evaluate(), 25);
		}

		[TestMethod]
		public void RunTheExampleFile() {
			var text = System.IO.File.ReadAllText("../../../Examples/example.ws");
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			var block = TokenParser.Parse(text, enviroment, CodePosition.GetExternal());
			block.Validate(enviroment);
			Assert.AreEqual(typeof(string), block.ReturnType);
			Assert.AreEqual("aaaab", block.Evaluate());
		}
	}
}


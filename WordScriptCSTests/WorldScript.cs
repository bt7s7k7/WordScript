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
			TokenParser.Parse("\"Comment\" .\nprint IN string 25 . .\nprint IN add \"Hello\" \"world\" . .\n  mul 5 10 , string , add \" = 25\" , print .", enviroment);
			var statements = enviroment._DebugDumpNodes();
			Assert.AreEqual(statements.Count, 4);
		}

		[TestMethod]
		public void VariableDefinition() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			Assert.IsNull(enviroment.GetVariable("y", CodePosition.GetExternal()));
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
			Assert.IsNotNull(enviroment.GetVariable("x", CodePosition.GetExternal()));
		}

		[TestMethod]
		[ExpectedException(typeof(VariableException))]
		public void VariableRedefinitionFail() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
		}

		[TestMethod]
		public void VariablesInCode() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.DefineVariable("x", typeof(int), CodePosition.GetExternal());
			TokenParser.Parse("DEFINE:y:int . &y . y= 0 . &x . x= &y .", enviroment);

			Assert.IsNotNull(enviroment.GetVariable("y", CodePosition.GetExternal()));
			Assert.AreEqual(enviroment.GetVariable("y", CodePosition.GetExternal()).Type, typeof(int));
		}

		[TestMethod]
		[ExpectedException(typeof(FunctionNotFoundException))]
		public void VariableTypeSafetyInCode() {
			Enviroment enviroment = new Enviroment(TypeInfoProvider.GetGlobal());
			enviroment.DefineVariable("x", typeof(string), CodePosition.GetExternal());
			TokenParser.Parse("x= 0 .", enviroment);
		}

		[TestMethod]
		public void StandardInclusion() {
			TypeInfoProvider provider = new TypeInfoProvider().LoadGlobals();

			Assert.IsNotNull(provider.GetFunction("string int"));
		}

		[TestMethod]
		[ExpectedException(typeof(FunctionNotFoundException))]
		public void StandardInclusionFiltering() {
			TypeInfoProvider provider = new TypeInfoProvider().LoadGlobals();

			Assert.IsNotNull(provider.GetFunction("TestMethod int"));
		}

	}
}

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WordScript;

namespace WordScriptTests {
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
		TypeInfoProvider provider = new TypeInfoProvider();

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
			var overloads = provider.GetOverloads("TestClass.from");
			Assert.AreEqual(overloads.Count, 1, "Function signature not found, error in GetOverloads or [TypeConversion]");
			var function = provider.GetFunction(overloads[0]);
			Assert.AreEqual(function.returnType, typeof(TestClass), "Function return value is not correct. Either a different overload is registered or error");
		}

		[TestMethod]
		public void Tokenizing() {
			var tokens = CodeTokenizer.Tokenize("print IN string.concat \"Hello\" \"world\" . .\n  int.mul 5 10 , int.toString , concat \" = 25\" , printn .");
			Assert.AreEqual(tokens.Count, 18);
		}
	}
}

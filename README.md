# WordScript

## Language guide
Word script source is made of a block made of statements.
Statements are made of words separated by a dot.
Words are strings separated by whitespace.

````
<name> [argument [...]] "."
````

You can join statements using a semicolon. This will put the output of the first statement as the first argument of the second statement.

````
<statement> "," <statement>
````
When joining multiple statements it's recommended to put each statement on a separated line.

````
mul 5 10 
, string 
, add " = 25" 
, print .
````

If the statement being piped into begins with a dot, the statement name gets prefixed with the name of the type of the output of the first statement.
````
int.mul 5 10 , .add 4 .
"is equivalent to:" .
int.mul 5 10 , int.add 4 .
````

A word identifies a function defined in the program embedding the language. There are special words meaning other things.
#### Number literals
````
<digit [...]>[suffix]
````
Number literals return their value. The suffix specifies the type of the number. Use `f` for floats and `i` for integers. If you don't specify a type, default type is integer.
#### String literals
````
"<...>"
'<...>'
````
String listerals return their value. A literal started with `"` is only ended with `"` and vice versa. You can use a number of escape codes:
````
\n  new line
\b  backspace
\r  carriage return
\'  '
\"  "
````
You can use a statement comprised of a string literal as a comment.
````
"This is a comment, since it's a literal that is not a argument, it doesn't do anything" .
````
#### Inline statements
````
"IN" <statement> "."
````
Evaluates the statement and then returns its value. Example: 
````
print IN add 5 10 . .
````
Notice the two terminators at the end. The first one terminates the inline statement and the other terminates the `print` statement.
#### Blocks and actions
````
"BLOCK" <statements> "END"
"ACTION" <statements> "END"
````
These keywords specify a block of statements that's returned. Statements must include a terminator. If you only specify one statement, the block will be of its type. Otherwise you must use a `return` statement to return a value. Actions will never have a return value. Everything returned will be discarded. Example:
````
array!int , .push 25 , .forEach DEFINE:&x:int ACTION print &x . END .
````
### Variables and blocks
Each block has a scope that contains variables. To use variables you must first define them using definitions. Definitions are statements.
````
DEFINE:<name>:<type>
DEFINE:<name> <value>
````
The first definition defines a value named `<name>` with type `<type>` containing a default value. The second definition sets the value to `<value>` and the type is infered from the type of the value.
Values can be set to new values and queried for their value. You can also obtain a reference to a value, for example to pass to `array!.forEach`.
````
<name>= <value>        setting variable value
&<name>                variable query
&&<name>               variable reference
````

Variables defined in the containing scope are inherited by the contained scope.

````
DEFINE:x:int .
&x . "<- you can use x here because it's defined in this scope" .
BLOCK
    &x . "<- you can use x here because it's defined in the containing scope" .
    DEFINE:y:int.
    &y . "<- you can use y here" .
END .  
&y . "<- This will not compile, because y is" .
     "not defined in this scope or the parent scope" .
````
### Standard functions
There are several functions that are avalible by default. However this can be disabled by the implementation.
````
#TODO: write the function signatures here.
````

Functions and types ended by `!` are generic. To use them you must provide a generic paramenter:
````
<function>!<type>
Example: array!int
````

### Example
````
"Comment" .

mul 5 10 
, string 
, add " = 25" 
, print .

print IN add 5 10 . .

array!int , .push 25 , .push "15" , .forEach DEFINE:&v:int ACTION print &v . END .

DEFINE:x:int .
x= 479 . "<- you can use x here because it's defined in this scope" .
BLOCK
    print &x .
    DEFINE:y "Hello world" .
	print &y .
END .

print IN string 45 8 56 56 true false "afaf" . .

return "aaaab" .
````

## Implementation guide for C#

Just copy the WordScript.cs file from WordScriptCS project and paste it into your project. To execute some code run: 
```` csharp
// Get a type provider, use the global one if you don't care about sandboxing
var provider = TypeInfoProvider.GetGlobal();
// Create the program enviroment
Enviroment enviroment = new Enviroment(provider);
// Compile the code
StatementBlock block = TokenParser.Parse("print 'hello world' .", enviroment, CodePosition.GetExternal());
// Run the code
object returnValue = block.Evaluate();
````
To register a type as a type avalible in type script use:
```` csharp
[WordScriptType]
class MyType {
    public string Foo() => "foo";
}

// or

provider.MapType(typeof(MyType));
````

To just register a function use:
```` csharp
class MyType {
    [FunctionDefinition("foo")]
    public static string Foo(string a) => a + "foo";
}
````
The function must be static or it will be ignored. 

# WordScript

## API
Word script source is made of words. Words are strings separated by whitespace.  Source code is made of statements. Statements are made of words separated by capital X or a new line.

````
<name> [argument [...]] ["X"]
````

You can join statements using a capital I or a pipe. This will put the output of the first statement as the first argument of the second statement. 

````
<statement> "I" | "|" <statement>
````
If the statement being piped into begins with a dot, the statement name gets prefixed with the name of the type of the output of the first statement.
````
int.mul 5 10 | .add 4
"is equivalent to:"
int.mul 5 10 | int.add 4
````

A word identifies a function defined in the program embedding the language. There are special words meaning other things.
````
<digit [...]>      number literal
"<...>"            string literal
'<...>'            also string literal
&<name>            variable reference
<name>=            variable setting
(<statement>)      expression
````

You can use a statement comprised of a string literal as a comment.
````
"This is a comment, since it's a literal that is not a argument, it doesn't do anything"
````

The interpreter automatically generates conversion functions: `<output type>.from <input type>`

### Example
````
print "Hello world"
print (string.concat "Hello" "world")
int.mul 5 10 | int.toString | concat " = 25" | print
"^ Prints 25 = 25"
````

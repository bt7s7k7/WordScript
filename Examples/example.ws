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

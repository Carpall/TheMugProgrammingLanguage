@[
	extern("CStruct_Person$ptri8$i8"),
	header("myheaders/person.c"),
	size(9),
	fields(ptr u8, u8)
]
type Person;

@[extern("ptri8$string_concat$ptrint8$ptrint8"), dynamiclib("mugstring.dll"), ]
func `+`(left: str, right: str): str;

@[export("add"), inline(false)]
func add(a: i32, b: i32): i32 { return a + b; }
type A { a: i32 }
type B { b: i64 }

type AB = ( A | B )

func main(): i32 {
  var node = new A { a: 10 } as AB
	
	return if node is A instance {
		instance.a
	} else {
		unbox<B>(node).b as i32
	}
}

func printf(text: str)
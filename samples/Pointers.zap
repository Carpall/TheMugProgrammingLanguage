#[
  Pointers Examples:
    - Only:
      - `new` operator
      - `&` operator
    can create a pointer
]#

import io;

type Person {
  name: str,
  age: u8
}

func (&self: Person) say(text: str) {
  println(text);
}

func main() {
  # allocated on the heap
  var me: *Person = new Person { name: "carpal", age: 16 };

  # allocated on the stack
  var me2: Person = Person { name: "carpal", age: 16 };

  # when call
  # pointers passed as copy
  me.say();

  # instances passed as reference
  me2.say();
}
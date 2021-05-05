import io;
import string;
import format;
import dropper;

type Person {
  name: str,
  age: u8
}

func hello(self: Person) {
  prinln(format("hi, I'm {0} and I'm {1} years old", new [str] { self.name, self.age.tostr() }));
}

func main() {
  var x = new ptr Person();
  x.hello();
  // drop it manually
  drop(ref x);
  // or dropped when out of scope
}
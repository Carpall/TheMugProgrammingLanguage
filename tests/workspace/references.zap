type Person { name: str, age: u8 }

func (self: &Person) change() {
  self.age = 0;
}

func main(): i32 {
  var x = new Person { age: 1 };
  (&x).change();
  return x.age as i32;
}

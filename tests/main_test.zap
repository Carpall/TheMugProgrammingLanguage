pub type A {
  pub x
  pub func x() {
  }
}

/*pub type Person {
  name: str
  age: u8

  pub func create(name: str, age: u8): Person { new Person { name: name, age: age } }

  pub func say(self, msg: str) { println!("{}: {}", self.name, msg) }
  pub func say_hello(self) { self.say("Hello, World!") }
}


pub enum AllocatorErr: err { size_is_zero }

pub type Allocator<T> {

  // fields

  data_with_refc: *(T, u64)
  deallocated: bool

  // static

  pub func create<T>(): AllocatorErr!Allocator<T> {
    return Allocator.create<T>(default<T>!())
  }

  pub func create<T>(value: T): AllocatorErr!Allocator<T> {
    const allocation = malloc(self.get_real_size()) as *(T, u64)
    (*allocation).item1 = value
    (*allocation).item2 = 1
    return new Allocator { data_with_refc: allocation }
  }

  // public

  pub func drop(self) {
    if self.deallocated { return }
    
    mfree(self.data_with_refc, self.get_real_size())
    self.deallocated = true
  }

  pub func get_real_size(self) { size<T>!() + size<u64>!() }

  // private

  func get_alloc(self): T { (*self.data_with_refc).item1 }
  func dereference(self) { if --(*self.data_with_refc).item2 == 0 { self.drop() } }
  func reference(self) { (*self.data_with_refc).item2++ }

  func `*`(): T {
    if self.deallocated { panic!("dereferenced nil pointer") } else { get_alloc() }
  }

  func `=`(self, allocator: Allocator): T { self.dereference() }           // assigned
  func `^`(self, ): Allocator             { self.reference() get_alloc() } // passed
  func `~`(self)                          { self.dereference() }           // out of scoped
}

func malloc(size: u64): unk
func mfree(ptr: unk, size: u64)

[test]
func test_allocate_defer_deallocate() {
  const ptr = Allocator.create<i32>()
  defer ptr.drop()

  print!("*ptr: {}", *ptr)
}

[test]
func test_allocate_implicit_deallocate() {
  const ptr = Allocator.create<i32>()

  print!("*ptr: {}", *ptr)
}
*/
import io;

pub type Stack<T> { raw: [T], len: i32, cap: i32 }

pub func Stack<T>(): Stack<T> { return Stack<T>(1000); }

pub func Stack<T>(capacity: i32): Stack<T> {
  return new Stack<T> {
    raw: new [T, capacity] { },
    len: 0,
    cap: capacity
  };
}

pub func (self: *Stack<T>) realloc<T>(capacity: i32) {
  (*self).cap = capacity;
  (*self).raw = realloc((*self).raw as unknown, capacity as i64) as [T];
}

pub func (self: Stack<T>) new_capacity<T>(): i32 {
  const MAX_CAP = 1000000000; # 1 miliardo
  
  if (self.cap*2 > MAX_CAP) { panic("reached max stack length"); }

  return self.cap*2;
}

pub func (self: &Stack<T>) push<T>(element: T) {

  if self.len >= self.cap { self.realloc<T>(self.new_capacity<T>()); }

  self.raw[self.len] = element;
  self.len++;
}

pub func (self: &Stack<T>) pop<T>(): T {
  if self.len == 0 { panic("called pop over empty stack"); }

  self.len--;
  return (*self).raw[self.len];
}

pub func realloc(allocation: unknown, size: i64): unknown;
pub func exit(code: i32);

pub func panic(err: str) {
  println(err);
  exit(1);
}

when test {
  func main(): i32 {
    var vec = Stack<i32>(1);

    (&vec).push<i32>(2);
    (&vec).push<i32>(3);
    
    return vec.len + (&vec).pop<i32>();
  }
}
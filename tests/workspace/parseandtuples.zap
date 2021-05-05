
// c wraps
func puts(text: str)

// implementation
enum ParseErr: u8 {
  InvalidNumber
  DoublePrefix
  PrefixInTheMiddle
}

func (self: ParseErr) to_str(): str {
  return
    if self == ParseErr.InvalidNumber { "NaN" }
    elif self == ParseErr.DoublePrefix { "DoublePrefix" }
    elif self == ParseErr.PrefixInTheMiddle { "PrefixInTheMiddle" }
    else { "UnkownError" }
}

func (self: chr) is_digit(): u1 {
  return (self as u8) >= ('0' as u8) & (self as u8) <= ('9' as u8)
}

func (self: str) parse_i32(): ParseErr!i32 {
  var res = 0
  var i = 0
	var prefixop = '\0'

	while true {
		const cur = self[i++]

    if cur == '\0' { break }
    
    if cur == '+' | cur == '-' {
      if prefixop != '\0' {
				return ParseErr.DoublePrefix
      }

      if i > 1 {
        return ParseErr.PrefixInTheMiddle
      }

			prefixop = cur
			continue
		}

		if !cur.is_digit() {
			return ParseErr.InvalidNumber
    }

		res = res * 10 + (cur as i32) - ('0' as i32)
	}
  
	if prefixop == '-' { res = -res }

  return res
}

/*
  tofix:
    in generic function + does not work (loop)
*/

func `+`(left: str, right: str): str {
  return ""
}

func main() {
  var self = Tuple<str, str, str>("", "", "")
  var x = self.item1 + self.item2 + self.item3
}

type Tuple<T, T1> {
  item1: T
  item2: T1
}

type Tuple<T, T1, T2> {
  item1: T
  item2: T1
  item3: T2
}

func Tuple<T, T1>(item1: T, item2: T1): Tuple<T, T1> {
  return new Tuple<T, T1> { item1: item1, item2: item2 }
}

func Tuple<T, T1, T2>(item1: T, item2: T1, item3: T2): Tuple<T, T1, T2> {
  return new Tuple<T, T1, T2> { item1: item1, item2: item2, item3: item3 }
}
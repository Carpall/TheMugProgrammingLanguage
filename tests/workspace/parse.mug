enum ParseErr: err { InvalidNumber DoublePrefix }

func (self: chr) is_digit(): bool {
  if (self as u8) >= ('0' as u8) { if (self as u8) <= ('9' as u8) { return true } }

  return false
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
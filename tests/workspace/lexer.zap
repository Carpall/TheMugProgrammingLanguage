import io;
import string;
import memory;

func strlen(text: str): i64;

func (self: chr) to_str(): str {
  const allocation = raw_alloc(2) as str;
  allocation[0] = self;
  allocation[1] = '\0';

  return allocation;
}

func (self: chr) is_upper(): u1 {
  const c = self as u8;

  if c >= 65 { if c <= 90 { return true; } } # & not implemented yet (and)
  
  return false;
}

func (self: chr) is_lower(): u1 {
  const c = self as u8;

  if c >= 97 { if c <= 122 { return true; } } # & not implemented yet (and)
  
  return false;
}

func (self: chr) is_alpha(): u1 {
  
  if self.is_upper() { return true; } # | not implemented yet (or)
  if self.is_lower() { return true; }

  return false;
}

func (self: chr) is_alpha_or(or: chr): u1 {
  if self.is_alpha() { return true; }
  if self == or { return true; }

  return false;
}

func (self: chr) is_num(): u1 {
  const c = self as u8;

  if c >= 48 { if c <= 57 { return true; } } # & not implemented yet

  return false;
}

func (self: chr) is_alphanum(): u1 {
  
  if self.is_alpha() { return true; }
  if self.is_num() { return true; }

  return false;
}

func (self: chr) is_alphanum_or(or: chr): u1 {
  if self.is_alphanum() { return true; }
  if self == or { return true; }

  return false;
}

func (self: chr) is_control(): u1 {
  
  if self == '\n' { return true; }
  if self == '\t' { return true; }
  if self == ' ' { return true; }

  return false;
}

type Lexer { src: str, idx: i32, len: i32 }

type Token { kind: TokenKind, value: str, pos: i32 }

enum TokenKind: u8 {
  Identifier: 0,
  Equal: 1,
  
  BAD: 100,
  EOF: 101
}

func Lexer(text: str): *Lexer {
  return
    alloc<Lexer>(
      new Lexer {
        src: text,
        len: strlen(text) as i32
      }
    );
}

func Token(kind: TokenKind, value: str, pos: i32): Token {
  return new Token { kind: kind, value: value, pos: pos };
}

func TokenEOF(pos: i32): Token {
  return Token(TokenKind.EOF, "<EOF>", pos);
}

func TokenBAD(value: chr, pos: i32): Token {
  return Token(TokenKind.BAD, value.to_str(), pos);
}

func (self: TokenKind) to_str(): str {
  if self == TokenKind.Identifier { return "TokenKind.Identifier"; }
  if self == TokenKind.Equal      { return "TokenKind.Equal"     ; }
  if self == TokenKind.BAD        { return "TokenKind.BAD"       ; }
  if self == TokenKind.EOF        { return "TokenKind.EOF"       ; }
  
  return "<?>";
}

func get_symbol_kind(curr: chr): TokenKind {
  if curr == '=' { return TokenKind.Equal; }
  
  return TokenKind.BAD;
}

func (self: Token) to_str(): str {
  return "Token { kind: " + self.kind.to_str() + ", value: " + self.value + ", pos: " + self.pos.to_str() + " }";
}

func (this: *Lexer) reached_eof(): u1 { return this.index() >= (*this).len; }

func (this: *Lexer) curr(): chr {
  if this.reached_eof() { return '\0'; }
  
  return (*this).src[this.index()];
}

func (this: *Lexer) advance() { (*this).idx++; }

func (this: *Lexer) unadvance() { (*this).idx--; }

func (this: *Lexer) skip_controls() {
  while true {
    if this.reached_eof() { break; }

    if !this.curr().is_control() { break; }

    this.advance();
  }
}

func (this: *Lexer) collect_identifier(): str {
  var result: str;

  while true {
    result += this.curr();

    this.advance();

    if this.reached_eof() { break; }

    if !this.curr().is_alphanum_or('_') { break; }
  }

  this.unadvance();

  return result;
}

func (this: *Lexer) index(): i32 { return (*this).idx; }

func (this: *Lexer) next_tok(): Token {
  this.skip_controls();
  
  if this.reached_eof() { return TokenEOF(this.index()); }

  var result = new Token { };
  const curr = this.curr();
  const pos = this.index();
  
  if curr.is_alpha_or('_') {
    result = Token(TokenKind.Identifier, this.collect_identifier(), pos);
  } else {
    result = Token(get_symbol_kind(curr), curr.to_str(), pos);
  }

  this.advance();

  return result;
}

func main() {
  const lexer = Lexer("c?okk_=");
  
  while !lexer.reached_eof() {
    println(lexer.next_tok().to_str());
  }
  
  lexer.free<Lexer>();
}
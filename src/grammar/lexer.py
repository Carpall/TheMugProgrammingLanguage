from typing_extensions import ParamSpec
from compilation import Component
from compilation.pos import Position
from grammar.token import Token

from os.path import abspath

keywords = [
  'fn',
  'let',
  'mut',
  'return',
  'struct',
  'if',
  'elif',
  'else',
  'while',
  'const',
  'enum',
  'new'
]

whitespace = [
  ' ',
  '\n',
  '\t',
]

symbols = [
  '+=',
  '-=',
  '*=',
  
  '==',
  '!=',
  '<',
  '>',
  '<=',
  '>=',

  '+',
  '-',
  '*',
  '/',

  '=',
  ':',
  ',',
  ';',

  '(', ')',
  '[', ']',
  '{', '}',

  '.',
  '&',
  '!',
  '..',
]

def keyword_or_identifier(identifier, pos):
  return Token(identifier, identifier, pos) if identifier in keywords else Token('ident', identifier, pos)

def is_identifier_char(char):
  return char.isalnum() or char == '_'

class Lexer(Component):
  def __init__(self, filename):
    filename = abspath(filename)
    super().__init__(filename)
    self.index = 0

    try:
      self.source = open(filename).read().replace('\r\n', '\n')
    except:
      self.report(f'bad filename \'{filename}\'')

  def reached_eof(self, index = None):
    if index == None:
      index = self.index

    return index >= len(self.source)

  def cur(self, index = None):
    if index == None:
      index = self.index
      
    if not self.reached_eof(index):
      return self.source[index]
    else:
      return '\0'

  def match(self, chars):
    if len(chars) == 1:
      return self.cur() == chars
    
    index = self.index
    for c in chars:
      if self.cur(index) != c:
        return False

      index += 1
    
    return True

  def match_eat(self, chars):
    r = self.match(chars)
    if r:
      self.advance(len(chars))
    
    return r

  def reached_eol(self):
    cur = self.cur()
    return cur == '\n' or cur == '\0'

  def advance(self, count = 1):
    self.index += count

  def eat_comments(self):
    if self.match_eat('//'):
      while True:
        self.advance()

        if self.reached_eol():
          break
      
      self.advance()

  def pos(self):
    return Position(self.source, self.filename, self.index)

  def match_identifier_char(self):
    return is_identifier_char(self.cur()) and not self.cur().isdigit()

  def collect_identifier(self):
    ident = ''
    while True:
      ident += self.cur()

      self.advance()

      cur = self.cur()
      if not is_identifier_char(cur) and not cur.isdigit():
        break
    
    self.advance(-1)
    return ident

  def lex(self):
    tokens = []
    while not self.reached_eof():
      token = self.next()
      if token != None:
        tokens.append(token)
    
    return tokens

  def eat_whitespace(self):
    while self.cur() in whitespace:
      self.advance()

  def match_digit(self):
    return self.cur().isdigit()

  def front(self):
    return self.cur(self.index + 1)

  def symbol_or_bad(self, pos):
    cur = self.cur()
    front = self.front()
    double = cur + front
    if double in symbols:
      self.advance()
      return Token(double, double, pos)
    elif cur in symbols:
      return Token(cur, cur, pos)
    else:
      return Token('bad', cur, pos)

  def collect_num(self):
    num = ''
    while True:
      num += self.cur()

      self.advance()

      cur = self.cur()
      
      if cur == '.' and self.cur(self.index + 1) != '.' and not '.' in num:
        continue

      if not cur.isdigit():
        break

    if num[len(num) - 1] == '.':
      num = num[:-1]
      self.advance(-1)
    
    if is_identifier_char(self.cur()):
      self.report(f'you need to separate the values', self.pos())

    self.advance(-1)
    return num

  def escape_char(self, char):
    try:
      return {
        '\'': '\'',
        '\\': '\\',
        'n': '\n',
        't': '\t'
      }[char]
    except:
      self.report('invalid escaped char', self.pos())

  def collect_string(self):
    str = ''
    while True:
      cur = self.cur()
      if cur == '\\':
        self.advance()
        cur = self.escape_char(self.cur())
        
      str += cur

      self.advance()

      if self.cur() == "'":
        break
    
    return str

  def next(self):
    self.eat_whitespace()
    self.eat_comments()
    self.eat_whitespace()

    if self.reached_eof():
      return None

    token = None
    pos = self.pos()

    if self.match_identifier_char():
      token = keyword_or_identifier(self.collect_identifier(), pos)
    elif self.match_digit():
      token = Token('num', self.collect_num(), pos)
    elif self.match_eat('\''):
      token = Token('str', self.collect_string(), pos)
    else:
      token = self.symbol_or_bad(pos)

    self.advance()
    return token
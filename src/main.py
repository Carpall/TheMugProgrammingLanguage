from compilation.component import CompilationException
from grammar import Lexer
from syntax import Parser
from backend import Emitter

import os

try:
  filename = 'tests/main.f'
  output_filename = filename + '.js'
  lexer = Lexer(filename)
  parser = Parser(lexer)
  emitter = Emitter(parser)
  result = emitter.emit() # parser.parse()

  with open(output_filename, 'w') as output:
    output.write(result)
  
  print(result)
  print('--------------OUTPUT--------------')
  os.system(f'node --trace-uncaught {output_filename}')
  # print(result)
  # for piece in result:
  #   print(f'{piece}\n')
except CompilationException as e:
  line, column = e.pos.get_line_column()
  print(f'error[L: {line}, C: {column}]:', e.pos.filename)
  print(f'{line} | {e.pos.get_source_line(line)}')
  print(' ' * (len(str(line)) + 2 + column) + '^', *e.args)
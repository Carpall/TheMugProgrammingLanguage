class Position:
  def __init__(self, source, filename, index):
    self.source = source
    self.filename = filename
    self.index = index
  
  def get_line_column(self):
    column = 1
    line = 1
    for index, c in enumerate(self.source):
      if index >= self.index:
        break

      if c == '\n':
        column = 1
        line += 1
      else:
        column += 1
    
    return line, column
  
  def get_source_line(self, line):
    return self.source.split('\n')[line-1]

  def __str__(self):
    return f'{self.index}'

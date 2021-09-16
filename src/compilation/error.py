class Error:
  def __init__(self, msg, pos = None):
    self.msg = msg
    self.pos = pos
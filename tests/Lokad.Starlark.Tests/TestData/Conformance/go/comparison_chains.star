# Comparison chaining should evaluate each operand once and avoid left-associative coercions.

assert_(1 < 2 < 3)
assert_(not (1 < 2 > 3))
assert_(1 < 2 == 2)
assert_(2 == 2 < 3)

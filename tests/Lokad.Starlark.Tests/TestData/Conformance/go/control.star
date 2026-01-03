# Subset of test_suite/testdata/go control flow coverage.

x = 0
if True:
  x = 1
elif False:
  fail("else of true")
else:
  fail("else of else of true")
assert_(x)
---
def loops():
  y = ""
  for x in [1, 2, 3, 4, 5]:
    if x == 2:
      continue
    if x == 4:
      break
    y = y + str(x)
  return y
assert_eq(loops(), "13")
---
g = 123
def f(x):
  for g in (1, 2, 3):
    if g == x:
      return g
assert_eq(f(2), 2)
assert_eq(f(4), None)
assert_eq(g, 123)

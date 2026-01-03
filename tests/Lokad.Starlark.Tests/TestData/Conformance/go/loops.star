# Subset of test_suite/testdata/go for loop/comprehension coverage.

assert_eq([2 * x for x in [1, 2, 3]], [2, 4, 6])
---
assert_eq([2 * x for x in [1, 2, 3] if x > 1], [4, 6])
---
assert_eq([(x, y) for x in [1, 2] for y in [3, 4]], [(1, 3), (1, 4), (2, 3), (2, 4)])
---
assert_eq([(x, y) for x in [1, 2] if x == 2 for y in [3, 4]], [(2, 3), (2, 4)])
---
assert_eq([x for x in {"a": 1, "b": 2}], ["a", "b"])
---
assert_eq([(y, x) for x, y in {1: 2, 3: 4}.items()], [(2, 1), (4, 3)])
---
assert_eq({x: x*x for x in range(3)}, {0: 0, 1: 1, 2: 4})
---
def f():
  res = []
  for (x, y), z in [(["a", "b"], 3), (["c", "d"], 4)]:
    res.append((x, y, z))
  return res
assert_eq(f(), [("a", "b", 3), ("c", "d", 4)])
---
def g():
  a = {}
  for i, a[i] in [("one", 1), ("two", 2)]:
    pass
  return a
assert_eq(g(), {"one": 1, "two": 2})

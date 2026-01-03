# Subset of test_suite/testdata/go dict coverage.

assert_eq({}, {})
assert_eq({"a": 1}, {"a": 1})
assert_({"a": 1})
assert_(not {})
---
d = {"a": 1, "b": 2}
assert_eq(d.get("a"), 1)
assert_eq(d.get("c"), None)
assert_eq(d.keys(), ["a", "b"])
assert_eq(d.values(), [1, 2])
---
d = {"a": 1, "b": 2}
assert_eq(d.pop("a"), 1)
assert_eq(d, {"b": 2})
assert_eq(d.pop("c", 3), 3)
assert_eq(d.pop("b"), 2)
assert_eq(len(d), 0)
---
d = {}
d["a"] = 1
d["b"] = 2
assert_eq(d["a"], 1)
assert_eq(d, {"a": 1, "b": 2})
---
d = {}
d["a"] ### (Key not found)
---
d = {"a": 1}
assert_eq(d.setdefault("a", 2), 1)
assert_eq(d.setdefault("b", 3), 3)
assert_eq(d, {"a": 1, "b": 3})
d.update({"b": 4, "c": 5})
assert_eq(d, {"a": 1, "b": 4, "c": 5})

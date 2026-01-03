# Subset of test_suite/testdata/go string coverage.

assert_eq("a\\bc", "a\\bc")
assert_("abc")
assert_(not "")
assert_eq("a" + "b", "ab")
assert_eq("ab" * 3, "ababab")
---
assert_eq("abc"[0], "a")
"abc"[3] ### (Index out of range)
---
assert_eq("banana".replace("a", "o", 1), "bonana")
assert_eq("foofoo".find("oo"), 1)
assert_eq("foofoo".rfind("oo"), 4)
---
assert_("foo".startswith("fo"))
assert_("foo".endswith("oo"))
---
assert_eq(",".join(["a", "b", "c"]), "a,b,c")
assert_eq("a.b.c".split("."), ["a", "b", "c"])
---
assert_eq("hello, world!".capitalize(), "Hello, world!")
assert_("hello, world".islower())
assert_(not "Catch-22".islower())
assert_("HAL-9000".isupper())
assert_(not "Catch-22".isupper())
assert_("Hello, World!".istitle())
assert_(not "HAL-9000".istitle())
assert_(" \t\r\n".isspace())
assert_(not "".isspace())
---
assert_eq("a".join("ctmrn".elems()), "catamaran")
assert_eq(list("abc".elems()), ["a", "b", "c"])
---
assert_eq("bonbon".index("on"), 1)
assert_eq("bonbon".index("on", 2), 4)
"bonbon".index("on", 2, 5) ### (substring not found)
assert_eq("bonbon".rindex("on"), 4)
assert_eq("bonbon".rindex("on", None, 5), 1)
---
assert_("base64".isalnum())
assert_(not "Catch-22".isalnum())
assert_("ABC".isalpha())
assert_(not "123".isalpha())
assert_("123".isdigit())
assert_(not "1.23".isdigit())
---
assert_eq("banana".removeprefix("ban"), "ana")
assert_eq("banana".removesuffix("ana"), "ban")
---
assert_eq(" a bc\n  def \t  ghi".split(), ["a", "bc", "def", "ghi"])
assert_eq(" a bc\n  def \t  ghi".split(None, 1), ["a", "bc\n  def \t  ghi"])
assert_eq(" a bc\n  def \t  ghi".rsplit(None, 1), [" a bc\n  def", "ghi"])
---
assert_eq("A\nB\rC\r\nD".splitlines(), ["A", "B", "C", "D"])
assert_eq("one\n\ntwo".splitlines(True), ["one\n", "\n", "two"])
---
assert_eq(hash("abc"), 96354)
---
assert_eq("%s %r" % ("hi", "hi"), 'hi "hi"')
assert_eq("%%d %d" % 1, "%d 1")
assert_eq("A %(foo)d %(bar)s Z" % {"foo": 123, "bar": "hi"}, "A 123 hi Z")
---
"%d %d" % 1 ### (Not enough arguments|not iterable)
---
"%d %d" % (1, 2, 3) ### (Too many arguments|not all arguments)

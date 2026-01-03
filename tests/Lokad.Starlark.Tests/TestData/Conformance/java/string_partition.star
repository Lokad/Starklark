# Subset of test_suite/testdata/java string_partition coverage.

assert_eq("lawl".partition("a"), ("l", "a", "wl"))
assert_eq("google".partition("o"), ("g", "o", "ogle"))
assert_eq("google".rpartition("o"), ("go", "o", "gle"))
assert_eq("google".partition("x"), ("google", "", ""))
assert_eq("google".rpartition("x"), ("", "", "google"))
---
"google".partition("") ### (separator cannot be empty)
---
"google".rpartition("") ### (separator cannot be empty)

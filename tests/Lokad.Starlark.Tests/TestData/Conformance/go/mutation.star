# Subset of go suite to cover mutation during iteration.

def mutate_list():
  values = [1, 2, 3]
  for value in values:
    values.append(value)

mutate_list() ### (Cannot mutate an iterable during iteration.)
---
def mutate_dict():
  items = {"a": 1, "b": 2}
  for key in items:
    items[key] = items[key] + 1

mutate_dict() ### (Cannot mutate an iterable during iteration.)

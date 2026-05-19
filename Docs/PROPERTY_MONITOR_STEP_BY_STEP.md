# Property Monitor Exercise

This guide walks you through one concrete next step: extending your simple monitoring setup so it can display a property, not only a field.

The goal is to keep the runtime tiny and understandable while proving that your panel can display multiple kinds of values.

## What You Should Have Already

- A working `DebugManager`.
- A working panel that can show a field value through `IMonitorHandle`.
- A `MonitorsPanel` that refreshes text every frame.
- A small runtime layer with `Monitor`, `MonitoringRegistry`, and `FieldMonitorHandle` or something equivalent.

If the field version is working, this exercise is the correct next step.

## Goal For This Step

By the end of this exercise you should be able to:

- mark a property with a monitoring attribute,
- discover that property at runtime,
- create a property handle,
- display the property value next to your field values,
- keep the UI code almost unchanged.

## Why This Step Matters

Fields are the simplest reflection case. Properties force you to separate the idea of "a monitored member" from "a specific member type".

That separation is important because later you will want to support:

- fields,
- properties,
- methods,
- static members,
- formatting rules.

If you can handle a property cleanly, the design is probably healthy enough to grow.

## Step 1: Choose One Example Property

Pick one existing sample class and add a simple property.

Good examples:

- `public float Health => currentHealth;`
- `public int Score => score;`
- `public bool IsAlive => health > 0;`

Keep it simple.

Prefer a property that:

- is cheap to read,
- returns a primitive type first,
- is not computed from a large amount of work.

The first property should prove the path, not stress it.

## Step 2: Decide How You Will Mark It

Use the same attribute concept you already use for fields.

Your property should be decorated so the runtime can discover it through reflection.

Your next design choice is whether the same attribute should work for both fields and properties.

For a small learning project, the best answer is yes.

That means your discovery logic should look for the same marker on both member types.

## Step 3: Add Property Discovery In The Runtime

Open your runtime discovery code.

You are looking for the place where fields are inspected and turned into handles.

Add a second path for properties.

The logic should be conceptually:

1. get the target type,
2. inspect its fields,
3. inspect its properties,
4. when a monitored property is found, create a property handle,
5. store that handle in the registry.

Do not try to make the property path fancy yet.

Just make it parallel to the field path.

## Step 4: Define A Property Handle

Create a new handle type dedicated to properties.

It should behave like your field handle and implement the same interface.

Minimum responsibilities:

- keep a reference to the target object,
- keep a reference to the `PropertyInfo`,
- expose a name,
- return the current value as a string.

The important part is that the panel should not care whether the value came from a field or property.

If the panel can read both through the same interface, your runtime is already doing the right separation.

## Step 5: Read The Property Value Safely

Inside the property handle, use the getter.

When you implement it, check these cases:

- the property has a getter,
- the property is readable,
- the target still exists,
- the getter returns something you can convert to text.

For your first version, it is fine to return `null` or a fallback string if something is missing.

Keep error handling simple.

The goal is to understand the flow, not to build a production-grade reflection wrapper yet.

## Step 6: Register The Property Handle

After you can create a property handle, connect it to the registry.

The registry should now do the same thing for fields and properties:

- create the handle,
- add it to the collection,
- expose it through `GetMonitorHandles()`.

If you have a method like `RegisterTarget(target)`, that is the place to extend.

The cleanest learning rule here is:

- one discovery method,
- multiple handle types,
- same output interface.

## Step 7: Add One Property To Your Example Class

Go back to the sample class you are using for testing.

Add one property and mark it with your monitor attribute.

Keep the existing field as well.

That way you can compare the behavior of both side by side.

You want to see:

- the field value,
- the property value,
- both updating in the panel.

## Step 8: Update The Panel To Render The New Handle

Your panel should already work with a list of handles.

If it consumes `IMonitorHandle`, then the panel probably does not need major changes.

Verify these points:

- the handle list contains both field and property entries,
- each handle has a visible name,
- each handle returns a readable string,
- the panel refresh loop updates both.

If the panel is hardcoded to field-only logic, stop and refactor it slightly so it consumes handles generically.

This is the boundary you want.

## Step 9: Verify The Result

Run the scene and confirm:

- the field appears,
- the property appears,
- the property value changes when the backing data changes,
- no exceptions are thrown every frame.

If something fails, use this checklist:

- Is the property public?
- Does it have a getter?
- Is it decorated with the expected attribute?
- Is the target object registered?
- Did the registry actually add the property handle?
- Is the panel reading the same registry instance?

## Step 10: Compare The Field And Property Paths

Once it works, pause and compare the code paths.

Ask these questions:

- What is identical between field and property handling?
- What is different only because of reflection member type?
- Which parts should stay separate?

Usually the answer becomes:

- shared: registration, storage, rendering, formatting,
- separate: value access (`FieldInfo.GetValue` vs `PropertyInfo.GetValue`).

That tells you where the abstraction should live.

## Step 11: Decide What To Build Next

After property support works, the next useful steps are:

- add method return support,
- add value formatting,
- add a better layout for the panel,
- add filtering by name or type,
- then clean up the runtime architecture.

Do not move to UI polish too early.

The property step is meant to prove the runtime architecture first.

## Recommended Learning Order

If you want to keep moving at a good pace, do this order:

1. field monitor,
2. property monitor,
3. method monitor,
4. simple formatting,
5. better UI.

That order keeps the system understandable while you expand it.

## Common Mistakes

- Trying to make one handle class support every member type immediately.
- Putting rendering logic into the registry.
- Letting the panel know too much about reflection.
- Adding UI polish before the runtime has proven itself.
- Skipping verification after each small change.

## What A Good Result Looks Like

You are in a good place when:

- the runtime discovers values correctly,
- the panel is only responsible for display,
- adding a new member type feels like adding one new handle class,
- the code is still easy to reason about.

That is the point where you can confidently move on to a better-looking UI.

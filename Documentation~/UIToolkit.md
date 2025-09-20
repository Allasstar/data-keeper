# DataKeeper.UIToolkit Documentation

## Overview

The `DataKeeper.UIToolkit` namespace is a comprehensive extension library for Unity's UI Toolkit system, providing fluent API extensions and utilities to streamline UI development. This namespace contains various extension classes that make it easier to work with Unity's `VisualElement` system by providing method chaining capabilities and simplified styling operations.

## Architecture

The namespace is organized into several specialized extension classes, each focusing on different aspects of UI element manipulation:

### Styling Extensions

- **UTKSizeExtensions** - Size, width, and height manipulation extensions
- **UTKLayoutExtensions** - Layout and positioning extensions
- **UTKSpacingExtensions** - Margin and padding extensions
- **UTKAppearanceExtensions** - Visual appearance and styling extensions
- **UTKTypographyExtensions** - Text and typography-related extensions

### Interaction Extensions

- **UTKHierarchyExtensions** - Element hierarchy manipulation extensions
- **UTKInteractionExtensions** - User interaction and event handling extensions

## Key Features

### 1. Fluent API Design

All extension methods follow a fluent API pattern, allowing for method chaining:

```csharp
element
    .SetWidth(200f)
    .SetHeight(100f)
    .SetMargin(10f);
```


### 2. Size Management (UTKSizeExtensions)

The `UTKSizeExtensions` class provides comprehensive size management capabilities:

#### Width Extensions
- `SetWidth(float width)` - Set width in pixels
- `SetWidth(float width, LengthUnit lengthUnit)` - Set width with specific unit
- `SetWidth(StyleKeyword styleKeyword)` - Set width using style keywords

#### Height Extensions
- `SetHeight(float height)` - Set height in pixels
- `SetHeight(float height, LengthUnit lengthUnit)` - Set height with specific unit
- `SetHeight(StyleKeyword styleKeyword)` - Set height using style keywords

#### Size Combinations
- `SetSize(float width, float height)` - Set both dimensions
- `SetSize(float size)` - Set square dimensions
- `SetSize(StyleKeyword styleKeyword)` - Apply style keyword to both dimensions

#### Constraints
- `SetMinWidth()`, `SetMinHeight()` - Set minimum dimensions
- `SetMaxWidth()`, `SetMaxHeight()` - Set maximum dimensions

#### Utilities
- `SetStretchToParent()` - Make element stretch to parent size

### 3. Multiple Unit Support

The extensions support various measurement units:
- **Pixels** - Direct pixel values
- **Percentage** - Percentage-based sizing using `LengthUnit.Percent`
- **Auto** - Automatic sizing using `StyleKeyword.Auto`
- **Initial** - Reset to initial values using `StyleKeyword.Initial`

### 4. Generic Type Constraints

All extension methods use generic type constraints `where T : VisualElement`, ensuring:
- Type safety
- Return of the original element type for continued chaining
- Compatibility with all `VisualElement` subclasses

## Usage Examples

### Basic Sizing

```csharp
// Set fixed dimensions
var button = new Button()
    .SetWidth(120f)
    .SetHeight(40f);

// Set percentage-based dimensions
var panel = new VisualElement()
    .SetWidth(50f, LengthUnit.Percent)
    .SetHeight(100f, LengthUnit.Percent);

// Set square element
var icon = new VisualElement()
    .SetSize(32f);

// Use auto sizing
var label = new Label()
    .SetWidth(StyleKeyword.Auto)
    .SetHeight(StyleKeyword.Auto);
```


### Size Constraints

```csharp
// Set minimum and maximum dimensions
var textField = new TextField()
    .SetMinWidth(100f)
    .SetMaxWidth(300f)
    .SetHeight(25f);
```


### Utility Methods

```csharp
// Make element stretch to fill parent
var background = new VisualElement()
    .SetStretchToParent();
```


## Best Practices

### 1. Method Chaining
Take advantage of the fluent API for readable, declarative UI construction:

```csharp
var dialog = new VisualElement()
    .SetSize(400f, 300f)
    .SetMinSize(300f, 200f)
    .AddToClassList("dialog");
```


### 2. Unit Consistency
Be consistent with measurement units within related elements:

```csharp
// Consistent percentage-based layout
var container = new VisualElement()
    .SetWidth(100f, LengthUnit.Percent)
    .SetHeight(50f, LengthUnit.Percent);
```


### 3. Responsive Design
Use percentage and auto sizing for responsive layouts:

```csharp
var responsivePanel = new VisualElement()
    .SetWidth(80f, LengthUnit.Percent)
    .SetHeight(StyleKeyword.Auto);
```


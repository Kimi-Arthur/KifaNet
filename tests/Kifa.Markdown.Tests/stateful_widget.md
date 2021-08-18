# StatefulWidget class [Null safety](https://dart.dev/null-safety)

A widget that has mutable state.

State is information that (1) can be read synchronously when the widget is built and (2) might change during the lifetime of the widget. It is the responsibility of the widget implementer to ensure that the [State](https://api.flutter.dev/flutter/widgets/State-class.html) is promptly notified when such state changes, using [State.setState](https://api.flutter.dev/flutter/widgets/State/setState.html).

A stateful widget is a widget that describes part of the user interface by building a constellation of other widgets that describe the user interface more concretely. The building process continues recursively until the description of the user interface is fully concrete (e.g., consists entirely of [RenderObjectWidget](https://api.flutter.dev/flutter/widgets/RenderObjectWidget-class.html)s, which describe concrete [RenderObject](https://api.flutter.dev/flutter/rendering/RenderObject-class.html)s).

Stateful widgets are useful when the part of the user interface you are describing can change dynamically, e.g. due to having an internal clock-driven state, or depending on some system state. For compositions that depend only on the configuration information in the object itself and the [BuildContext](https://api.flutter.dev/flutter/widgets/BuildContext-class.html) in which the widget is inflated, consider using [StatelessWidget](https://api.flutter.dev/flutter/widgets/StatelessWidget-class.html).

[StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html) instances themselves are immutable and store their mutable state either in separate [State](https://api.flutter.dev/flutter/widgets/State-class.html) objects that are created by the [createState](https://api.flutter.dev/flutter/widgets/StatefulWidget/createState.html) method, or in objects to which that [State](https://api.flutter.dev/flutter/widgets/State-class.html) subscribes, for example [Stream](https://api.flutter.dev/flutter/dart-async/Stream-class.html) or [ChangeNotifier](https://api.flutter.dev/flutter/foundation/ChangeNotifier-class.html) objects, to which references are stored in final fields on the [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html) itself.

The framework calls [createState](https://api.flutter.dev/flutter/widgets/StatefulWidget/createState.html) whenever it inflates a [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html), which means that multiple [State](https://api.flutter.dev/flutter/widgets/State-class.html) objects might be associated with the same [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html) if that widget has been inserted into the tree in multiple places. Similarly, if a [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html) is removed from the tree and later inserted in to the tree again, the framework will call [createState](https://api.flutter.dev/flutter/widgets/StatefulWidget/createState.html) again to create a fresh [State](https://api.flutter.dev/flutter/widgets/State-class.html) object, simplifying the lifecycle of [State](https://api.flutter.dev/flutter/widgets/State-class.html) objects.

A [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html) keeps the same [State](https://api.flutter.dev/flutter/widgets/State-class.html) object when moving from one location in the tree to another if its creator used a [GlobalKey](https://api.flutter.dev/flutter/widgets/GlobalKey-class.html) for its [key](https://api.flutter.dev/flutter/widgets/Widget/key.html). Because a widget with a [GlobalKey](https://api.flutter.dev/flutter/widgets/GlobalKey-class.html) can be used in at most one location in the tree, a widget that uses a [GlobalKey](https://api.flutter.dev/flutter/widgets/GlobalKey-class.html) has at most one associated element. The framework takes advantage of this property when moving a widget with a global key from one location in the tree to another by grafting the (unique) subtree associated with that widget from the old location to the new location (instead of recreating the subtree at the new location). The [State](https://api.flutter.dev/flutter/widgets/State-class.html) objects associated with [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html) are grafted along with the rest of the subtree, which means the [State](https://api.flutter.dev/flutter/widgets/State-class.html) object is reused (instead of being recreated) in the new location. However, in order to be eligible for grafting, the widget must be inserted into the new location in the same animation frame in which it was removed from the old location.

## Performance considerations

There are two primary categories of [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html)s.

The first is one which allocates resources in [State.initState](https://api.flutter.dev/flutter/widgets/State/initState.html) and disposes of them in [State.dispose](https://api.flutter.dev/flutter/widgets/State/dispose.html), but which does not depend on [InheritedWidget](https://api.flutter.dev/flutter/widgets/InheritedWidget-class.html)s or call [State.setState](https://api.flutter.dev/flutter/widgets/State/setState.html). Such widgets are commonly used at the root of an application or page, and communicate with subwidgets via [ChangeNotifier](https://api.flutter.dev/flutter/foundation/ChangeNotifier-class.html)s, [Stream](https://api.flutter.dev/flutter/dart-async/Stream-class.html)s, or other such objects. Stateful widgets following such a pattern are relatively cheap (in terms of CPU and GPU cycles), because they are built once then never update. They can, therefore, have somewhat complicated and deep build methods.

The second category is widgets that use [State.setState](https://api.flutter.dev/flutter/widgets/State/setState.html) or depend on [InheritedWidget](https://api.flutter.dev/flutter/widgets/InheritedWidget-class.html)s. These will typically rebuild many times during the application's lifetime, and it is therefore important to minimize the impact of rebuilding such a widget. (They may also use [State.initState](https://api.flutter.dev/flutter/widgets/State/initState.html) or [State.didChangeDependencies](https://api.flutter.dev/flutter/widgets/State/didChangeDependencies.html) and allocate resources, but the important part is that they rebuild.)

There are several techniques one can use to minimize the impact of rebuilding a stateful widget:

- Push the state to the leaves. For example, if your page has a ticking clock, rather than putting the state at the top of the page and rebuilding the entire page each time the clock ticks, create a dedicated clock widget that only updates itself.
- Minimize the number of nodes transitively created by the build method and any widgets it creates. Ideally, a stateful widget would only create a single widget, and that widget would be a [RenderObjectWidget](https://api.flutter.dev/flutter/widgets/RenderObjectWidget-class.html). (Obviously this isn't always practical, but the closer a widget gets to this ideal, the more efficient it will be.)
- If a subtree does not change, cache the widget that represents that subtree and re-use it each time it can be used. It is massively more efficient for a widget to be re-used than for a new (but identically-configured) widget to be created. Factoring out the stateful part into a widget that takes a child argument is a common way of doing this.
- Use `const` widgets where possible. (This is equivalent to caching a widget and re-using it.)
- Avoid changing the depth of any created subtrees or changing the type of any widgets in the subtree. For example, rather than returning either the child or the child wrapped in an [IgnorePointer](https://api.flutter.dev/flutter/widgets/IgnorePointer-class.html), always wrap the child widget in an [IgnorePointer](https://api.flutter.dev/flutter/widgets/IgnorePointer-class.html) and control the [IgnorePointer.ignoring](https://api.flutter.dev/flutter/widgets/IgnorePointer/ignoring.html) property. This is because changing the depth of the subtree requires rebuilding, laying out, and painting the entire subtree, whereas just changing the property will require the least possible change to the render tree (in the case of [IgnorePointer](https://api.flutter.dev/flutter/widgets/IgnorePointer-class.html), for example, no layout or repaint is necessary at all).
- If the depth must be changed for some reason, consider wrapping the common parts of the subtrees in widgets that have a [GlobalKey](https://api.flutter.dev/flutter/widgets/GlobalKey-class.html) that remains consistent for the life of the stateful widget. (The [KeyedSubtree](https://api.flutter.dev/flutter/widgets/KeyedSubtree-class.html) widget may be useful for this purpose if no other widget can conveniently be assigned the key.)

This is a skeleton of a stateful widget subclass called `YellowBird`.

In this example. the [State](https://api.flutter.dev/flutter/widgets/State-class.html) has no actual state. State is normally represented as private member fields. Also, normally widgets have more constructor arguments, each of which corresponds to a `final` property.

```dart
class YellowBird extends StatefulWidget {
  const YellowBird({ Key? key }) : super(key: key);

  @override
  _YellowBirdState createState() => _YellowBirdState();
}

class _YellowBirdState extends State<YellowBird> {
  @override
  Widget build(BuildContext context) {
    return Container(color: const Color(0xFFFFE306));
  }
}
```





[*link*](https://api.flutter.dev/flutter/#)

Sample

This example shows the more generic widget `Bird` which can be given a color and a child, and which has some internal state with a method that can be called to mutate it:

*assignment*

```dart
class Bird extends StatefulWidget {
  const Bird({
    Key? key,
    this.color = const Color(0xFFFFE306),
    this.child,
  }) : super(key: key);

  final Color color;
  final Widget? child;

  @override
  _BirdState createState() => _BirdState();
}

class _BirdState extends State<Bird> {
  double _size = 1.0;

  void grow() {
    setState(() { _size += 0.1; });
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: widget.color,
      transform: Matrix4.diagonal3Values(_size, _size, 1.0),
      child: widget.child,
    );
  }
}
```



By convention, widget constructors only use named arguments. Also by convention, the first argument is [key](https://api.flutter.dev/flutter/widgets/Widget/key.html), and the last argument is `child`, `children`, or the equivalent.

See also:

- [State](https://api.flutter.dev/flutter/widgets/State-class.html), where the logic behind a [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html) is hosted.
- [StatelessWidget](https://api.flutter.dev/flutter/widgets/StatelessWidget-class.html), for widgets that always build the same way given a particular configuration and ambient state.
- [InheritedWidget](https://api.flutter.dev/flutter/widgets/InheritedWidget-class.html), for widgets that introduce ambient state that can be read by descendant widgets.

- Inheritance: [Object](https://api.flutter.dev/flutter/dart-core/Object-class.html) [DiagnosticableTree](https://api.flutter.dev/flutter/foundation/DiagnosticableTree-class.html) [Widget](https://api.flutter.dev/flutter/widgets/Widget-class.html) StatefulWidget

- Implementers: [ActionListener](https://api.flutter.dev/flutter/widgets/ActionListener-class.html)[Actions](https://api.flutter.dev/flutter/widgets/Actions-class.html)[AndroidView](https://api.flutter.dev/flutter/widgets/AndroidView-class.html)[AnimatedCrossFade](https://api.flutter.dev/flutter/widgets/AnimatedCrossFade-class.html)[AnimatedList](https://api.flutter.dev/flutter/widgets/AnimatedList-class.html)[AnimatedSwitcher](https://api.flutter.dev/flutter/widgets/AnimatedSwitcher-class.html)[AnimatedWidget](https://api.flutter.dev/flutter/widgets/AnimatedWidget-class.html)[AppBar](https://api.flutter.dev/flutter/material/AppBar-class.html)[AutofillGroup](https://api.flutter.dev/flutter/widgets/AutofillGroup-class.html)[AutomaticKeepAlive](https://api.flutter.dev/flutter/widgets/AutomaticKeepAlive-class.html)[BackButtonListener](https://api.flutter.dev/flutter/widgets/BackButtonListener-class.html)[BottomAppBar](https://api.flutter.dev/flutter/material/BottomAppBar-class.html)[BottomNavigationBar](https://api.flutter.dev/flutter/material/BottomNavigationBar-class.html)[BottomSheet](https://api.flutter.dev/flutter/material/BottomSheet-class.html)[ButtonStyleButton](https://api.flutter.dev/flutter/material/ButtonStyleButton-class.html)[CalendarDatePicker](https://api.flutter.dev/flutter/material/CalendarDatePicker-class.html)[Checkbox](https://api.flutter.dev/flutter/material/Checkbox-class.html)[CupertinoActivityIndicator](https://api.flutter.dev/flutter/cupertino/CupertinoActivityIndicator-class.html)[CupertinoApp](https://api.flutter.dev/flutter/cupertino/CupertinoApp-class.html)[CupertinoButton](https://api.flutter.dev/flutter/cupertino/CupertinoButton-class.html)[CupertinoContextMenu](https://api.flutter.dev/flutter/cupertino/CupertinoContextMenu-class.html)[CupertinoContextMenuAction](https://api.flutter.dev/flutter/cupertino/CupertinoContextMenuAction-class.html)[CupertinoDatePicker](https://api.flutter.dev/flutter/cupertino/CupertinoDatePicker-class.html)[CupertinoNavigationBar](https://api.flutter.dev/flutter/cupertino/CupertinoNavigationBar-class.html)[CupertinoPageScaffold](https://api.flutter.dev/flutter/cupertino/CupertinoPageScaffold-class.html)[CupertinoPicker](https://api.flutter.dev/flutter/cupertino/CupertinoPicker-class.html)[CupertinoSearchTextField](https://api.flutter.dev/flutter/cupertino/CupertinoSearchTextField-class.html)[CupertinoSegmentedControl](https://api.flutter.dev/flutter/cupertino/CupertinoSegmentedControl-class.html)[CupertinoSlider](https://api.flutter.dev/flutter/cupertino/CupertinoSlider-class.html)[CupertinoSlidingSegmentedControl](https://api.flutter.dev/flutter/cupertino/CupertinoSlidingSegmentedControl-class.html)[CupertinoSliverNavigationBar](https://api.flutter.dev/flutter/cupertino/CupertinoSliverNavigationBar-class.html)[CupertinoSliverRefreshControl](https://api.flutter.dev/flutter/cupertino/CupertinoSliverRefreshControl-class.html)[CupertinoSwitch](https://api.flutter.dev/flutter/cupertino/CupertinoSwitch-class.html)[CupertinoTabScaffold](https://api.flutter.dev/flutter/cupertino/CupertinoTabScaffold-class.html)[CupertinoTabView](https://api.flutter.dev/flutter/cupertino/CupertinoTabView-class.html)[CupertinoTextField](https://api.flutter.dev/flutter/cupertino/CupertinoTextField-class.html)[CupertinoTimerPicker](https://api.flutter.dev/flutter/cupertino/CupertinoTimerPicker-class.html)[DatePickerDialog](https://api.flutter.dev/flutter/material/DatePickerDialog-class.html)[DateRangePickerDialog](https://api.flutter.dev/flutter/material/DateRangePickerDialog-class.html)[DefaultTabController](https://api.flutter.dev/flutter/material/DefaultTabController-class.html)[Dismissible](https://api.flutter.dev/flutter/widgets/Dismissible-class.html)[Draggable](https://api.flutter.dev/flutter/widgets/Draggable-class.html)[DraggableScrollableSheet](https://api.flutter.dev/flutter/widgets/DraggableScrollableSheet-class.html)[DragTarget](https://api.flutter.dev/flutter/widgets/DragTarget-class.html)[DrawerController](https://api.flutter.dev/flutter/material/DrawerController-class.html)[DropdownButton](https://api.flutter.dev/flutter/material/DropdownButton-class.html)[DualTransitionBuilder](https://api.flutter.dev/flutter/widgets/DualTransitionBuilder-class.html)[EditableText](https://api.flutter.dev/flutter/widgets/EditableText-class.html)[ExpandIcon](https://api.flutter.dev/flutter/material/ExpandIcon-class.html)[ExpansionPanelList](https://api.flutter.dev/flutter/material/ExpansionPanelList-class.html)[ExpansionTile](https://api.flutter.dev/flutter/material/ExpansionTile-class.html)[FlexibleSpaceBar](https://api.flutter.dev/flutter/material/FlexibleSpaceBar-class.html)[Focus](https://api.flutter.dev/flutter/widgets/Focus-class.html)[FocusableActionDetector](https://api.flutter.dev/flutter/widgets/FocusableActionDetector-class.html)[FocusTraversalGroup](https://api.flutter.dev/flutter/widgets/FocusTraversalGroup-class.html)[Form](https://api.flutter.dev/flutter/widgets/Form-class.html)[FormField](https://api.flutter.dev/flutter/widgets/FormField-class.html)[FutureBuilder](https://api.flutter.dev/flutter/widgets/FutureBuilder-class.html)[GlowingOverscrollIndicator](https://api.flutter.dev/flutter/widgets/GlowingOverscrollIndicator-class.html)[Hero](https://api.flutter.dev/flutter/widgets/Hero-class.html)[Image](https://api.flutter.dev/flutter/widgets/Image-class.html)[ImplicitlyAnimatedWidget](https://api.flutter.dev/flutter/widgets/ImplicitlyAnimatedWidget-class.html)[Ink](https://api.flutter.dev/flutter/material/Ink-class.html)[InputDatePickerFormField](https://api.flutter.dev/flutter/material/InputDatePickerFormField-class.html)[InputDecorator](https://api.flutter.dev/flutter/material/InputDecorator-class.html)[InteractiveViewer](https://api.flutter.dev/flutter/widgets/InteractiveViewer-class.html)[LicensePage](https://api.flutter.dev/flutter/material/LicensePage-class.html)[ListWheelScrollView](https://api.flutter.dev/flutter/widgets/ListWheelScrollView-class.html)[Localizations](https://api.flutter.dev/flutter/widgets/Localizations-class.html)[Material](https://api.flutter.dev/flutter/material/Material-class.html)[MaterialApp](https://api.flutter.dev/flutter/material/MaterialApp-class.html)[MergeableMaterial](https://api.flutter.dev/flutter/material/MergeableMaterial-class.html)[MonthPicker](https://api.flutter.dev/flutter/material/MonthPicker-class.html)[MouseRegion](https://api.flutter.dev/flutter/widgets/MouseRegion-class.html)[NavigationRail](https://api.flutter.dev/flutter/material/NavigationRail-class.html)[Navigator](https://api.flutter.dev/flutter/widgets/Navigator-class.html)[NestedScrollView](https://api.flutter.dev/flutter/widgets/NestedScrollView-class.html)[Overlay](https://api.flutter.dev/flutter/widgets/Overlay-class.html)[PageView](https://api.flutter.dev/flutter/widgets/PageView-class.html)[PaginatedDataTable](https://api.flutter.dev/flutter/material/PaginatedDataTable-class.html)[PlatformViewLink](https://api.flutter.dev/flutter/widgets/PlatformViewLink-class.html)[PopupMenuButton](https://api.flutter.dev/flutter/material/PopupMenuButton-class.html)[PopupMenuEntry](https://api.flutter.dev/flutter/material/PopupMenuEntry-class.html)[ProgressIndicator](https://api.flutter.dev/flutter/material/ProgressIndicator-class.html)[Radio](https://api.flutter.dev/flutter/material/Radio-class.html)[RangeSlider](https://api.flutter.dev/flutter/material/RangeSlider-class.html)[RawAutocomplete](https://api.flutter.dev/flutter/widgets/RawAutocomplete-class.html)[RawChip](https://api.flutter.dev/flutter/material/RawChip-class.html)[RawGestureDetector](https://api.flutter.dev/flutter/widgets/RawGestureDetector-class.html)[RawKeyboardListener](https://api.flutter.dev/flutter/widgets/RawKeyboardListener-class.html)[RawMaterialButton](https://api.flutter.dev/flutter/material/RawMaterialButton-class.html)[RawScrollbar](https://api.flutter.dev/flutter/widgets/RawScrollbar-class.html)[RefreshIndicator](https://api.flutter.dev/flutter/material/RefreshIndicator-class.html)[ReorderableList](https://api.flutter.dev/flutter/widgets/ReorderableList-class.html)[ReorderableListView](https://api.flutter.dev/flutter/material/ReorderableListView-class.html)[RestorationScope](https://api.flutter.dev/flutter/widgets/RestorationScope-class.html)[RootRestorationScope](https://api.flutter.dev/flutter/widgets/RootRestorationScope-class.html)[Router](https://api.flutter.dev/flutter/widgets/Router-class.html)[Scaffold](https://api.flutter.dev/flutter/material/Scaffold-class.html)[ScaffoldMessenger](https://api.flutter.dev/flutter/material/ScaffoldMessenger-class.html)[Scrollable](https://api.flutter.dev/flutter/widgets/Scrollable-class.html)[Scrollbar](https://api.flutter.dev/flutter/material/Scrollbar-class.html)[SelectableText](https://api.flutter.dev/flutter/material/SelectableText-class.html)[SemanticsDebugger](https://api.flutter.dev/flutter/widgets/SemanticsDebugger-class.html)[Shortcuts](https://api.flutter.dev/flutter/widgets/Shortcuts-class.html)[Slider](https://api.flutter.dev/flutter/material/Slider-class.html)[SliverAnimatedList](https://api.flutter.dev/flutter/widgets/SliverAnimatedList-class.html)[SliverAppBar](https://api.flutter.dev/flutter/material/SliverAppBar-class.html)[SliverReorderableList](https://api.flutter.dev/flutter/widgets/SliverReorderableList-class.html)[SnackBar](https://api.flutter.dev/flutter/material/SnackBar-class.html)[SnackBarAction](https://api.flutter.dev/flutter/material/SnackBarAction-class.html)[StatefulBuilder](https://api.flutter.dev/flutter/widgets/StatefulBuilder-class.html)[StatusTransitionWidget](https://api.flutter.dev/flutter/widgets/StatusTransitionWidget-class.html)[Stepper](https://api.flutter.dev/flutter/material/Stepper-class.html)[StreamBuilderBase](https://api.flutter.dev/flutter/widgets/StreamBuilderBase-class.html)[TabBar](https://api.flutter.dev/flutter/material/TabBar-class.html)[TabBarView](https://api.flutter.dev/flutter/material/TabBarView-class.html)[TextField](https://api.flutter.dev/flutter/material/TextField-class.html)[TextSelectionGestureDetector](https://api.flutter.dev/flutter/widgets/TextSelectionGestureDetector-class.html)[Tooltip](https://api.flutter.dev/flutter/material/Tooltip-class.html)[UiKitView](https://api.flutter.dev/flutter/widgets/UiKitView-class.html)[UniqueWidget](https://api.flutter.dev/flutter/widgets/UniqueWidget-class.html)[UserAccountsDrawerHeader](https://api.flutter.dev/flutter/material/UserAccountsDrawerHeader-class.html)[ValueListenableBuilder](https://api.flutter.dev/flutter/widgets/ValueListenableBuilder-class.html)[WidgetInspector](https://api.flutter.dev/flutter/widgets/WidgetInspector-class.html)[WidgetsApp](https://api.flutter.dev/flutter/widgets/WidgetsApp-class.html)[WillPopScope](https://api.flutter.dev/flutter/widgets/WillPopScope-class.html)[YearPicker](https://api.flutter.dev/flutter/material/YearPicker-class.html)

## Constructors

- [StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget/StatefulWidget.html)({[Key](https://api.flutter.dev/flutter/foundation/Key-class.html)? key})

  Initializes `key` for subclasses.const

## Properties

- *[hashCode](https://api.flutter.dev/flutter/widgets/Widget/hashCode.html)* → [int](https://api.flutter.dev/flutter/dart-core/int-class.html)

  The hash code for this object. [[...\]](https://api.flutter.dev/flutter/widgets/Widget/hashCode.html)@[nonVirtual](https://api.flutter.dev/flutter/meta/nonVirtual-constant.html), read-only, inherited

- *[key](https://api.flutter.dev/flutter/widgets/Widget/key.html)* → [Key](https://api.flutter.dev/flutter/foundation/Key-class.html)?

  Controls how one widget replaces another widget in the tree. [[...\]](https://api.flutter.dev/flutter/widgets/Widget/key.html)final, inherited

- *[runtimeType](https://api.flutter.dev/flutter/dart-core/Object/runtimeType.html)* → [Type](https://api.flutter.dev/flutter/dart-core/Type-class.html)

  A representation of the runtime type of the object.read-only, inherited

## Methods

- [createElement](https://api.flutter.dev/flutter/widgets/StatefulWidget/createElement.html)() → [StatefulElement](https://api.flutter.dev/flutter/widgets/StatefulElement-class.html)

  Creates a [StatefulElement](https://api.flutter.dev/flutter/widgets/StatefulElement-class.html) to manage this widget's location in the tree. [[...\]](https://api.flutter.dev/flutter/widgets/StatefulWidget/createElement.html)override

- [createState](https://api.flutter.dev/flutter/widgets/StatefulWidget/createState.html)() → [State](https://api.flutter.dev/flutter/widgets/State-class.html)<[StatefulWidget](https://api.flutter.dev/flutter/widgets/StatefulWidget-class.html)>

  Creates the mutable state for this widget at a given location in the tree. [[...\]](https://api.flutter.dev/flutter/widgets/StatefulWidget/createState.html)@[factory](https://api.flutter.dev/flutter/meta/factory-constant.html), @[protected](https://api.flutter.dev/flutter/meta/protected-constant.html)

- *[debugDescribeChildren](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/debugDescribeChildren.html)*() → [List](https://api.flutter.dev/flutter/dart-core/List-class.html)<[DiagnosticsNode](https://api.flutter.dev/flutter/foundation/DiagnosticsNode-class.html)>

  Returns a list of [DiagnosticsNode](https://api.flutter.dev/flutter/foundation/DiagnosticsNode-class.html) objects describing this node's children. [[...\]](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/debugDescribeChildren.html)@[protected](https://api.flutter.dev/flutter/meta/protected-constant.html), inherited

- *[debugFillProperties](https://api.flutter.dev/flutter/widgets/Widget/debugFillProperties.html)*([DiagnosticPropertiesBuilder](https://api.flutter.dev/flutter/foundation/DiagnosticPropertiesBuilder-class.html) properties) → void

  Add additional properties associated with the node. [[...\]](https://api.flutter.dev/flutter/widgets/Widget/debugFillProperties.html)inherited

- *[noSuchMethod](https://api.flutter.dev/flutter/dart-core/Object/noSuchMethod.html)*([Invocation](https://api.flutter.dev/flutter/dart-core/Invocation-class.html) invocation) → dynamic

  Invoked when a non-existent method or property is accessed. [[...\]](https://api.flutter.dev/flutter/dart-core/Object/noSuchMethod.html)inherited

- *[toDiagnosticsNode](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/toDiagnosticsNode.html)*({[String](https://api.flutter.dev/flutter/dart-core/String-class.html)? name, [DiagnosticsTreeStyle](https://api.flutter.dev/flutter/foundation/DiagnosticsTreeStyle-class.html)? style}) → [DiagnosticsNode](https://api.flutter.dev/flutter/foundation/DiagnosticsNode-class.html)

  Returns a debug representation of the object that is used by debugging tools and by [DiagnosticsNode.toStringDeep](https://api.flutter.dev/flutter/foundation/DiagnosticsNode/toStringDeep.html). [[...\]](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/toDiagnosticsNode.html)inherited

- *[toString](https://api.flutter.dev/flutter/foundation/Diagnosticable/toString.html)*({[DiagnosticLevel](https://api.flutter.dev/flutter/foundation/DiagnosticLevel-class.html) minLevel = DiagnosticLevel.info}) → [String](https://api.flutter.dev/flutter/dart-core/String-class.html)

  A string representation of this object. [[...\]](https://api.flutter.dev/flutter/foundation/Diagnosticable/toString.html)inherited

- *[toStringDeep](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/toStringDeep.html)*({[String](https://api.flutter.dev/flutter/dart-core/String-class.html) prefixLineOne = '', [String](https://api.flutter.dev/flutter/dart-core/String-class.html)? prefixOtherLines, [DiagnosticLevel](https://api.flutter.dev/flutter/foundation/DiagnosticLevel-class.html) minLevel = DiagnosticLevel.debug}) → [String](https://api.flutter.dev/flutter/dart-core/String-class.html)

  Returns a string representation of this node and its descendants. [[...\]](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/toStringDeep.html)inherited

- *[toStringShallow](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/toStringShallow.html)*({[String](https://api.flutter.dev/flutter/dart-core/String-class.html) joiner = ', ', [DiagnosticLevel](https://api.flutter.dev/flutter/foundation/DiagnosticLevel-class.html) minLevel = DiagnosticLevel.debug}) → [String](https://api.flutter.dev/flutter/dart-core/String-class.html)

  Returns a one-line detailed description of the object. [[...\]](https://api.flutter.dev/flutter/foundation/DiagnosticableTree/toStringShallow.html)inherited

- *[toStringShort](https://api.flutter.dev/flutter/widgets/Widget/toStringShort.html)*() → [String](https://api.flutter.dev/flutter/dart-core/String-class.html)

  A short, textual description of this widget.inherited

## Operators

- *[operator ==](https://api.flutter.dev/flutter/widgets/Widget/operator_equals.html)*([Object](https://api.flutter.dev/flutter/dart-core/Object-class.html) other) → [bool](https://api.flutter.dev/flutter/dart-core/bool-class.html)

  The equality operator. [[...\]](https://api.flutter.dev/flutter/widgets/Widget/operator_equals.html)@[nonVirtual](https://api.flutter.dev/flutter/meta/nonVirtual-constant.html), inherited


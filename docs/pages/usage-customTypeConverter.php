<!DOCTYPE html>
<html>

<head>
    <title>Custom Type Converter</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">Custom Type Converters</h1>
    <hr>
    <p>This page describes how to make your own type converters. A custom type converter lets you serialize and deserialize a certain type of object exactly how you want.</p>
    <p><b>NOTE: You must read <a href="#" data-navigates="alongside" data-navigateTo="Basic Conversion">Basic Conversion</a> and <a href="#" data-navigates="alongside" data-navigateTo="Library Structure">Library Structure</a> before reading this.</p>

    <h2 id="creation">Creating a Converter</h2>
    <h3 id="creation-converterClass">Converter Class</h3>
    <hr>

    <p>The first thing to do is make the class the converter will be located in. To do this, make a new class that extends <code>ABSaveTypeConverter</code>. Then, make a static instance of the class like below (call this <code>Instance</code>), this is the standard way to make converters in ABSave, and means you only need one instance that can be reused. You should also enforce the usage of this by making the constructor private.</p>

    <pre><code class="language-csharp">
public class MyConverter : ABSaveTypeConverter
{
    public static readonly MyConverter Instance = new MyConverter();
    private MyConverter() { }
}
    </code></pre>

    <h3 id="creation-typeSelection">Type Selection</h3>
    <hr>

    <p>The next thing you must implement is what type your converter converts.</p>
    <p>ABSave provides two ways to do tell it what type your converter wants:</p>
    
    <ul>
        <li><p><b>Exact Type</b> - If your converter serializes one exact type (for example <code>MyClass</code>), then you can tell ABSave the exact type it serializes.</p></li>
        <li><p><b>Non-exact Type</b> - If your converter doesn't serialize an exact type, and it's a type that can vary (for example <code>MyClass&lt;T&gt;</code>), then you can tell ABSave the exact type it serializes.</p></li>
    </ul>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>
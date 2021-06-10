<!DOCTYPE html>
<html>

<head>
    <title>Binary Format</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">Binary Output</h1>
    <hr>
    <p>
        This part contains documentation about how the binary format of ABSave is structured.<br><br>
        If you want to know how to <i>use</i> the C# library for ABSave, you most likely want the <a href="#" data-navigates="alongside" data-navigaeTo="Usage">Usage</a> section.
    </p>

    <h2>Subsections</h2>
    <hr>
    <p>It's recommended you read "Document Structure", and use "Built-in Types" as a reference is necessary</p>
    <div class="navBoxContainer">
        <div data-navigates="child" data-navigateTo="Document Structure" class="navBox navBoxLight navBox-half">
            <h1 class="noAnchor">Document Structure</h1>
            <p>The general structure of ABSave text.</p>
        </div>
        <div data-navigates="child" data-navigateTo="Built-in Types" class="navBox navBoxLight navBox-half">
            <h1 class="noAnchor">Built-in Types</h1>
            <p>How each of the built-in supported types are serialized.</p>
        </div>
    </div>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>
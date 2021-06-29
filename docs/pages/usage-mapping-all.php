<!DOCTYPE html>
<html>

<head>
    <title>Mapping - All</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">All Features</h1>
    <hr>
    <p>This page gives you a list of all of the different features and the respective attributes available for them.</p>

    <h2 id="member-ordering">Member ordering</h2>
    <hr>
    <p>This part covers what exactly the number in the <code>Save</code> attributes mean.</p>
    <p>The number in the <code>Save</code> attribute essentially defines the 'order' in which the properties' values are put in the file</p>
    <p>When ABSave is serialized, it doesn't store the names of every property like a format such as JSON, it instead stores <i>only the values</i> of those properties, meaning ABSave uses the order those properties appear in to know which value goes to which property.</p>
    <p>The numbers in the <code>Save</code> attribute determine this order.</p>
    <p>Quite simply, ABSave will sort the properties based on these numbers in ascending order.</p>

    <p>Here are some things you may want to see:</p>

    <h2 id="inheritance">Inheritance</h2>
    <hr>
    <p>To be filled in</p>

    <h2 id="fields">Field Serialization</h2>
    <hr>
    <p>To be filled in</p>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>
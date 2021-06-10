<!DOCTYPE html>
<html lang="en">
<head>
    <title> ABSave Docs </title>
    <?php include('../base/docsHead.html') ?>

    <script>
        StartPath = "pages/start.php";

        Sections = [
            new Section("Binary Output", "pages/binaryFormat.php", "images/BinaryFormatImg.png", [
                new Section("Document Structure", "pages/binaryFormat-documentStructure.php", "", []),
                new Section("Built-in Types", "pages/binaryFormat-builtInTypes.php", "", []),
            ]),
            new Section("Usage", "pages/usage.php", "../img/Usage.png", [
                new Section("Basic Conversion", "pages/usage-basics.php", "", []),
                new Section("Library Structure", "pages/usage-structure.php", "", []),
                new Section("Custom Type Converter", "pages/usage-customTypeConverter.php", "", [])
            ])
        ];
    </script>
</head>
<body class="lightTheme">

    <?php include('../base/docsBody.html') ?>

</body>
</html>
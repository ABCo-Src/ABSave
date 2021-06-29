<!DOCTYPE html>
<html lang="en">
<head>
    <title> ABSave Docs </title>
    <?php include('../base/docsHead.html') ?>

    <script>
        StartPath = "pages/start.php";

        Sections = [
            new Section("Using ABSave", "pages/usage.php", "../img/Usage.png", [
                new Section("Quick Start", "pages/usage-basics.php", "", []),
                new Section("Settings", "pages/usage-settings.php", "", []),
                new Section("Mapping", "pages/usage-mapping.php", "", [
                    new Section("All Features", "pages/usage-mapping-all.php", "", []),
                    new Section("More Versioning", "pages/usage-mapping-versioning.php", "", []),
                    new Section("Via Settings", "pages/usage-mapping-settings.php", "", []),
                ])
            ]),
            new Section("Binary Output", "pages/binaryFormat.php", "images/BinaryFormatImg.png", [
                new Section("Document Structure", "pages/binaryFormat-documentStructure.php", "", []),
                new Section("Built-in Types", "pages/binaryFormat-builtInTypes.php", "", [])
            ])
        ];
    </script>
</head>
<body class="lightTheme">

    <?php include('../base/docsBody.html') ?>

</body>
</html>
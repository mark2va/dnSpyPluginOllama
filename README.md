# dnSpyPluginOllama

Скомпилируйте проект в Visual Studio

Скопируйте DnSpyAIRefactor.dll в папку dnSpy\Bin\Plugins

Убедитесь, что в этой же папке есть:

Newtonsoft.Json.dll

DnSpyAIRefactor.dll.manifest

Использование:
Запустите dnSpy

Откройте любой .NET assembly

В контекстном меню (правый клик) на любом элементе кода появится:

AI Refactor Method

AI Refactor Class

AI Refactor Property

AI Refactor Variable

В дереве assembly появится "AI Batch Refactor Module"

Настройка:
Конфигурация сохраняется в %APPDATA%\dnSpy\AI_Refactor_Config.json

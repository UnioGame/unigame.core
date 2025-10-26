namespace UniGame.Editor
{
    /// <summary>
    /// Типы ресурсов для включения в link.xml
    /// </summary>
    public enum LinkXmlResourceType
    {
        RegexPattern,     // Регулярное выражение для проверки типов
        BaseType,         // Базовый тип + все наследники
        Namespace,        // Все типы в namespace
        Assembly,         // Все типы в assembly
        ConcreteType      // Конкретный тип
    }
}
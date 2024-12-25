# **DataKeeper**

**DataKeeper** is a comprehensive package/Unity extension that enhances the functionality and convenience of Unity development. It includes a collection of scripts designed to streamline common tasks and improve efficiency. From reactive variables and preferences to data serialization and registration systems, DataKeeper offers a wide range of tools to simplify your workflow.


# [OpenUPM](https://openupm.com/packages/com.micrarriors.data-keeper/)
`https://openupm.com/packages/com.micrarriors.data-keeper/`


# **Install via Package Manager**

Please follow the instrustions:

-   open  **Edit/Project Settings/Package Manager**
-   add a new Scoped Registry (or edit the existing OpenUPM entry)
    
    Name
    `package.openupm.com`
    
    URL
    `https://package.openupm.com`
    
    Scope(s)
    `com.micrarriors.data-keeper`
    
-   click  **Save**  or  **Apply**
-   open  **Window/Package Manager**
-   click  **+**
-   select  **Add package by name...**  or  **Add package from git URL...**
-   paste  `com.micrarriors.data-keeper`  into name
-   paste  `#.#.#`  into version (example: 0.6.0)
-   click  **Add**


# --- Documentation ---

## [Reactive<T>](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md)

The `Reactive<T>` class, located within the `DataKeeper.Generic` namespace, provides a generic reactive data type that can track and trigger events when its value changes. This feature is useful in scenarios where you want to maintain and observe the state of a value reactively.


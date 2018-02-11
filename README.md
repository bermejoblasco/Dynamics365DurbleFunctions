# Dynamics365DurbleFunctions
Ejemplo de inserción múltiple en Dynamics 365 mediante Durable Functions.<br/>

Para este ejemplo es necesario:<br/>
1 - Tener un cuenta de Dynamics 365<br/>
2 - Subscripción de Azure. No es necesaria, se puede lanzar en local, pero es mejor poderla lanzar en Azure<br/>

Pasos a seguir.<br/>
1 - Lanzar el proceso CreateFiles
    Este proceso nos crea mil ficheros con 50 elementos del tipo account.
2 - Subir los 50 ficheros a un blob storage.
3 - Crear una Queue con el nombre operation2.
4 - En el proyecto VSSample ir a Counter.cs
    En el método CreateOrganizationService en la variable discoveryUi poner la url del Dynamics365, en userCredentials.UserName.UserName poner el usuario y en userCredentials.UserName.Password el password
5 - Si se ejecuta en local, ejecutar el poryecto VSSample, sino hacer el deploy en Azure y configurar las cadenas de conexión del storage y activar application insight.
6 - Por último, añadir un elemento con el string "2" en la queue.

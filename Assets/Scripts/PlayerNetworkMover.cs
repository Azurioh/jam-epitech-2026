using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetworkMover : NetworkBehaviour
{
    public float speed = 5f;

    // Cette fonction est appelée automatiquement quand l'objet apparaît sur le réseau
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // On se déplace à une position aléatoire pour ne pas être sur les autres
            MoveToRandomPosition();
        }
    }

    void MoveToRandomPosition()
    {
        Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        transform.position = randomPosition;
    }

    void Update()
    {
        if (!IsOwner) return;

        Vector3 moveDir = Vector3.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveDir.z = +1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveDir.z = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveDir.x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveDir.x = +1f;
        }

        transform.position += moveDir.normalized * speed * Time.deltaTime;
    }
}

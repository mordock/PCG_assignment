﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour {
    public int boardRows, boardColumns;
    public int minRoomSize, maxRoomSize;
    public float minimumSafeDistance = 15f;
    [Range(0.0f, 15)]
    public int maxAmountOfEnemies;
    public GameObject floorTile;
    public GameObject corridorTile;

    public GameObject Player;
    public GameObject Door;
    public GameObject Wall;
    public GameObject Enemy;
    public GameObject Key;
    public GameObject Flag;
    public GameObject Sword;

    private GameObject[,] boardPositionsFloor;
    private int playerSpawn = 0;
    private int enemySpawn = 0;
    private int doorSpawn = 0;
    private int keySpawn = 0;
    public int maxKeyDoorValue;
    private int flagSpawn = 0;
    private int swordSpawn = 0;

    private Vector2 playerstartPos;
    private Vector2 lastenemyPos = new Vector2(0, 0);
    private Vector2 lastdoorPos = new Vector2(0, 0);

    public class SubDungeon {
        public SubDungeon left, right;
        public Rect rect;
        public Rect room = new Rect(-1, -1, 0, 0); // i.e null
        public int debugId;
        public List<Rect> corridors = new List<Rect>();

        private static int debugCounter = 0;

        public SubDungeon(Rect mrect) {
            rect = mrect;
            debugId = debugCounter;
            debugCounter++;
        }

        public bool IAmLeaf() {
            return left == null && right == null;
        }

        public bool Split(int minRoomSize, int maxRoomSize) {
            if (!IAmLeaf()) {
                return false;
            }

            // choose a vertical or horizontal split depending on the proportions
            // i.e. if too wide split vertically, or too long horizontally, 
            // or if nearly square choose vertical or horizontal at random
            bool splitH;
            if (rect.width / rect.height >= 1.25) {
                splitH = false;
            } else if (rect.height / rect.width >= 1.25) {
                splitH = true;
            } else {
                splitH = Random.Range(0.0f, 1.0f) > 0.5;
            }

            if (Mathf.Min(rect.height, rect.width) / 2 < minRoomSize) {
                Debug.Log("Sub-dungeon " + debugId + " will be a leaf");



                return false;
            }

            if (splitH) {
                // split so that the resulting sub-dungeons widths are not too small
                // (since we are splitting horizontally) 
                int split = Random.Range(minRoomSize, (int)(rect.width - minRoomSize));

                left = new SubDungeon(new Rect(rect.x, rect.y, rect.width, split));
                right = new SubDungeon(
                    new Rect(rect.x, rect.y + split, rect.width, rect.height - split));
            } else {
                int split = Random.Range(minRoomSize, (int)(rect.height - minRoomSize));

                left = new SubDungeon(new Rect(rect.x, rect.y, split, rect.height));
                right = new SubDungeon(
                    new Rect(rect.x + split, rect.y, rect.width - split, rect.height));
            }

            return true;
        }

        public void CreateRoom() {
            if (left != null) {
                left.CreateRoom();
            }

            if (right != null) {
                right.CreateRoom();
            }

            if (left != null && right != null) {
                CreateCorridorBetween(left, right);
            }

            if (IAmLeaf()) {
                int roomWidth = (int)Random.Range(rect.width / 2, rect.width - 2);
                int roomHeight = (int)Random.Range(rect.height / 2, rect.height - 2);
                int roomX = (int)Random.Range(1, rect.width - roomWidth - 1);
                int roomY = (int)Random.Range(1, rect.height - roomHeight - 1);

                // room position will be absolute in the board, not relative to the sub-dungeon
                room = new Rect(rect.x + roomX, rect.y + roomY, roomWidth, roomHeight);
                Debug.Log("Created room " + room + " in sub-dungeon " + debugId + " " + rect);
            }
        }


        public void CreateCorridorBetween(SubDungeon left, SubDungeon right) {
            Rect lroom = left.GetRoom();
            Rect rroom = right.GetRoom();

            Debug.Log("Creating corridor(s) between " + left.debugId + "(" + lroom + ") and " + right.debugId + " (" +
                      rroom + ")");

            // attach the corridor to a random point in each room
            Vector2 lpoint = new Vector2((int)Random.Range(lroom.x + 1, lroom.xMax - 1),
                (int)Random.Range(lroom.y + 1, lroom.yMax - 1));
            Vector2 rpoint = new Vector2((int)Random.Range(rroom.x + 1, rroom.xMax - 1),
                (int)Random.Range(rroom.y + 1, rroom.yMax - 1));

            // always be sure that left point is on the left to simplify the code
            if (lpoint.x > rpoint.x) {
                Vector2 temp = lpoint;
                lpoint = rpoint;
                rpoint = temp;
            }

            int w = (int)(lpoint.x - rpoint.x);
            int h = (int)(lpoint.y - rpoint.y);

            Debug.Log("lpoint: " + lpoint + ", rpoint: " + rpoint + ", w: " + w + ", h: " + h);

            // if the points are not aligned horizontally
            if (w != 0) {
                // choose at random to go horizontal then vertical or the opposite
                if (Random.Range(0, 1) > 2) {
                    // add a corridor to the right
                    corridors.Add(new Rect(lpoint.x, lpoint.y, Mathf.Abs(w) + 1, 1));

                    // if left point is below right point go up
                    // otherwise go down
                    if (h < 0) {
                        corridors.Add(new Rect(rpoint.x, lpoint.y, 1, Mathf.Abs(h)));
                    } else {
                        corridors.Add(new Rect(rpoint.x, lpoint.y, 1, -Mathf.Abs(h)));
                    }
                } else {
                    // go up or down
                    if (h < 0) {
                        corridors.Add(new Rect(lpoint.x, lpoint.y, 1, Mathf.Abs(h)));
                    } else {
                        corridors.Add(new Rect(lpoint.x, rpoint.y, 1, Mathf.Abs(h)));
                    }

                    // then go right
                    corridors.Add(new Rect(lpoint.x, rpoint.y, Mathf.Abs(w) + 1, 1));
                }
            } else {
                // if the points are aligned horizontally
                // go up or down depending on the positions
                if (h < 0) {
                    corridors.Add(new Rect((int)lpoint.x, (int)lpoint.y, 1, Mathf.Abs(h)));
                } else {
                    corridors.Add(new Rect((int)rpoint.x, (int)rpoint.y, 1, Mathf.Abs(h)));
                }
            }

            Debug.Log("Corridors: ");
            foreach (Rect corridor in corridors) {
                Debug.Log("corridor: " + corridor);
            }
        }

        public Rect GetRoom() {
            if (IAmLeaf()) {
                return room;
            }

            if (left != null) {
                Rect lroom = left.GetRoom();
                if (lroom.x != -1) {
                    return lroom;
                }
            }

            if (right != null) {
                Rect rroom = right.GetRoom();
                if (rroom.x != -1) {
                    return rroom;
                }
            }

            // workaround non nullable structs
            return new Rect(-1, -1, 0, 0);
        }
    }

    public void CreateBSP(SubDungeon subDungeon) {
        Debug.Log("Splitting sub-dungeon " + subDungeon.debugId + ": " + subDungeon.rect);
        if (subDungeon.IAmLeaf()) {
            // if the sub-dungeon is too large split it
            if (subDungeon.rect.width > maxRoomSize
                || subDungeon.rect.height > maxRoomSize
                || Random.Range(0.0f, 1.0f) > 0.25) {

                if (subDungeon.Split(minRoomSize, maxRoomSize)) {
                    Debug.Log("Splitted sub-dungeon " + subDungeon.debugId + " in "
                              + subDungeon.left.debugId + ": " + subDungeon.left.rect + ", "
                              + subDungeon.right.debugId + ": " + subDungeon.right.rect);


                    CreateBSP(subDungeon.left);
                    CreateBSP(subDungeon.right);
                }
            }
        }
    }

    public void DrawRooms(SubDungeon subDungeon) {
        if (subDungeon == null) {
            return;
        }

        if (subDungeon.IAmLeaf()) {
            for (int i = (int)subDungeon.room.x; i < subDungeon.room.xMax; i++) {
                for (int j = (int)subDungeon.room.y; j < subDungeon.room.yMax; j++) {
                    GameObject instance =
                        Instantiate(floorTile, new Vector3(i, j, 0f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, j] = instance;
                    RaycastHit2D hit = Physics2D.Raycast(new Vector2(i, j), new Vector2(i, j), 0);

                    //player
                    if (playerSpawn != 1) {
                        if (hit.collider != null && hit.collider.gameObject.tag != "Wall") {
                            Instantiate(Player,
                                new Vector3(Convert.ToInt32((subDungeon.room.xMin + subDungeon.room.xMax) / 2), Convert.ToInt32((subDungeon.room.yMin + subDungeon.room.yMax) / 2), -1f), Quaternion.identity);
                            playerstartPos = new Vector2(Convert.ToInt32((subDungeon.room.xMin + subDungeon.room.xMax) / 2), Convert.ToInt32((subDungeon.room.yMin + subDungeon.room.yMax) / 2));
                            playerSpawn++;
                        }
                    }

                }
            }
        } else {
            DrawRooms(subDungeon.left);
            DrawRooms(subDungeon.right);
        }
    }

    void DrawCorridors(SubDungeon subDungeon) {
        if (subDungeon == null) {
            return;
        }

        DrawCorridors(subDungeon.left);
        DrawCorridors(subDungeon.right);

        foreach (Rect corridor in subDungeon.corridors) {
            for (int i = (int)corridor.x; i < corridor.xMax; i++) {
                for (int j = (int)corridor.y; j < corridor.yMax; j++) {
                    if (boardPositionsFloor[i, j] == null) {
                        GameObject instance =
                            Instantiate(corridorTile, new Vector3(i, j, 0f), Quaternion.identity) as GameObject;
                        instance.transform.SetParent(transform);
                        boardPositionsFloor[i, j] = instance;
                    }
                }
            }
        }
    }

    void Start() {
        SubDungeon rootSubDungeon = new SubDungeon(new Rect(0, 0, boardRows, boardColumns));
        CreateBSP(rootSubDungeon);
        rootSubDungeon.CreateRoom();

        boardPositionsFloor = new GameObject[boardRows, boardColumns];
        DrawRooms(rootSubDungeon);
        DrawCorridors(rootSubDungeon);

        //create walls
        for (int i = 0; i < boardRows; i++) {
            for (int j = 0; j < boardColumns; j++) {
                RaycastHit2D hit = Physics2D.Raycast(new Vector2(i, j), new Vector2(i, j), 0);

                if (hit.collider == null) {
                    Instantiate(Wall, new Vector3(i, j, 1f), Quaternion.identity);
                }
            }
        }

        // create doors, enemies, keys
        for (int i = 0; i < boardRows; i++) {
            for (int j = 0; j < boardColumns; j++) {
                RaycastHit2D hitup = Physics2D.Raycast(new Vector2(i, j + 1), new Vector2(i, j + 1), 0);
                RaycastHit2D hitdown = Physics2D.Raycast(new Vector2(i, j - 1), new Vector2(i, j - 1), 0);
                RaycastHit2D hitright = Physics2D.Raycast(new Vector2(i + 1, j), new Vector2(i + 1, j), 0);
                RaycastHit2D hitleft = Physics2D.Raycast(new Vector2(i - 1, j), new Vector2(i - 1, j), 0);
                RaycastHit2D hit = Physics2D.Raycast(new Vector2(i, j), new Vector2(i, j), 0);

                //door
                if (doorSpawn != maxKeyDoorValue) {
                    if (hitup.collider != null && hitdown.collider != null) {
                        if (hitup.collider.gameObject.tag.Equals("Wall") &&
                            hitdown.collider.gameObject.tag.Equals("Wall") &&
                            hit.collider.gameObject.tag.Equals("Ground")) {
                            if (lastdoorPos.y != j) {
                                Instantiate(Door, new Vector3(i, j, -1f), Quaternion.identity);
                                lastdoorPos = new Vector2(i, j);
                                doorSpawn++;
                            }
                        }
                    }
                }

                //keys
                if (keySpawn != maxKeyDoorValue) {
                    if (hit.collider.gameObject.tag.Equals("Player")) {
                        Instantiate(Key, new Vector3(i, j - 1, -1f), Quaternion.identity);
                        keySpawn++;
                    }

                    if (hit.collider.gameObject.tag.Equals("Ground")) {
                        float random = Random.Range(0f, 1f);
                        if (random <= .01f) {
                            Instantiate(Key, new Vector3(i, j, -1f), Quaternion.identity);
                            keySpawn++;
                        }
                    }
                }

                //enemy
                if (hitup.collider != null && hitdown.collider != null && hitright.collider != null &&
                    hitleft.collider != null) {
                    if (hitup.collider.gameObject.tag.Equals("Ground") &&
                        hitdown.collider.gameObject.tag.Equals("Ground") &&
                        hit.collider.gameObject.tag.Equals("Ground") &&
                        hitleft.collider.gameObject.tag.Equals("Ground") &&
                        hitright.collider.gameObject.tag.Equals("Ground")) {

                        if (enemySpawn <= maxAmountOfEnemies) {
                            if (Vector2.Distance(playerstartPos, new Vector2(i, j)) >= minimumSafeDistance) {
                                if (Vector2.Distance(lastenemyPos, new Vector2(i, j)) >= 5) {
                                    Instantiate(Enemy, new Vector3(i, j, -1f), Quaternion.identity);
                                    lastenemyPos = new Vector2(i, j);
                                    enemySpawn++;
                                }
                            }
                        }
                    }
                }

                //flag
                float distance = Vector2.Distance(hit.transform.position, playerstartPos);
                if (flagSpawn < 1) {
                    if (distance >= ((boardColumns + boardRows) * .4)) {
                        if (hit.collider.gameObject.tag.Equals("Ground")) {
                            Instantiate(Flag, new Vector3(i, j, -1f), Quaternion.identity);
                            flagSpawn++;
                        }
                    }
                }

                //swords
                if (swordSpawn <= maxAmountOfEnemies) {
                    if (hit.collider.gameObject.tag.Equals("Player")) {
                        Instantiate(Sword, new Vector3(i + 1, j, -1f), Quaternion.identity);
                        Instantiate(Sword, new Vector3(i - 1, j, -1f), Quaternion.identity);
                        swordSpawn++;
                    }
                    if (hit.collider.gameObject.tag.Equals("Ground")) {
                        float random = Random.Range(0f, 1f);
                        if (random <= .01f) {
                            Instantiate(Sword, new Vector3(i, j, -1f), Quaternion.identity);
                            swordSpawn++;
                        }
                    }
                }
            }
        }
    }


    void Update() {
        if (Input.GetKeyDown("r")) {
            SceneManager.LoadScene("Assignment");
        }
    }
}
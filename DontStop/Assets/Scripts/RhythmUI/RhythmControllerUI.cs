using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RhythmControllerUI : MonoBehaviour
{
    public float hitTime; //in fraction of a note
    public GameObject notePrefab;
    public AudioSource musicPlayer;
    public TextAsset patternMapJSON;
    public BoxCollider2D hitZone;

    public static RhythmControllerUI instance { get; private set; }
    public bool hasStarted = false;
    public Vector2 noteBufferPosition;
    public bool noteInHitArea = false;
    public float noteDespawnDelay;

    public PatternMap patternMap;

    private float speed;
    private float BPM;
    //private float firstNoteDistance;
    //private List<float> distanceVector;
    //private int distanceVectorIndex = 0;
    private float firstNoteTime;
    private List<float> timeVector;
    private int timeVectorIndex = 0;

    private RectTransform rectTransform;

    private List<GameObject> noteBufer;
    private List<NoteUI> noteScripts;
    private List<RectTransform> noteRectTransforms;
    private int nextNotes = 0;
    private int previousNotes = -1;
    private bool pulsatingPlayed;

    public float usableBarFraction = 15f/17f;
    private float barWidth;

    /*private float startOKTime = 0;
    private float endOKTime = 0;
    private int nextTimeIndex = 0;*/

    private void Awake()
    {
        instance = this;
        rectTransform = GetComponent<RectTransform>();
        barWidth = rectTransform.sizeDelta.x * usableBarFraction;

        //for now I generate the pattern map here;
        patternMap = GeneratePatternMap();

        speed = patternMap.noteSpeed;
        BPM = patternMap.BPM;
        timeVectorIndex = patternMap.numberOfNotesSkippedOnFirstPlay;
        GenerateTimeVector();

        GenerateNotes();

        GenerateHitZones();
    }

    private void GenerateHitZones()
    {
        float noteLength = speed * (60/BPM);

        hitZone.size = new Vector2(noteLength * hitTime, 50);

        noteDespawnDelay = (60/BPM) * hitTime;
    }

    /*private void GenerateDistanceVector()
    {
        List<float> absoluteDistanceVector = new List<float>();

        float measureLength = (speed / (BPM/60)) * patternMap.tempoDenominator;
        float baseDistance = 0;
        foreach(BeatPattern bp in patternMap.pattern)
        {
            foreach(float notePosition in bp.notePositions)
            {
                if (notePosition >= 0 && notePosition < bp.numMeasures)
                {
                    absoluteDistanceVector.Add(baseDistance + measureLength * notePosition);
                }
            }
            baseDistance += measureLength * bp.numMeasures;
        }

        absoluteDistanceVector.Sort();

        firstNoteDistance = absoluteDistanceVector[0] + patternMap.initialDelay*speed;

        distanceVector = new List<float>(absoluteDistanceVector.Capacity);
        for (int i = 0; i < absoluteDistanceVector.Count-1; i++)
        {
            distanceVector.Add(absoluteDistanceVector[i + 1] - absoluteDistanceVector[i]);
        }
        distanceVector.Add(absoluteDistanceVector[0] + (baseDistance - absoluteDistanceVector[absoluteDistanceVector.Count - 1]));

        //print(distanceVector.Count);
        //foreach (var a in distanceVector)
            //print(a);
    }*/

    private void GenerateTimeVector()
    {
        timeVector = new List<float>();
        float measureTime = (60/BPM) * patternMap.tempoDenominator;
        float baseTime = 0;
        foreach (BeatPattern bp in patternMap.pattern)
        {
            foreach (float notePosition in bp.notePositions)
            {
                if (notePosition >= 0 && notePosition < bp.numMeasures)
                {
                    timeVector.Add(baseTime + measureTime * notePosition);
                }
            }
            baseTime += measureTime * bp.numMeasures;
        }

        timeVector.Sort();

        firstNoteTime = timeVector[0] + patternMap.initialDelay;

        //print(timeVector.Count);
        //foreach (var a in timeVector)
            //print(a);
    }

    private PatternMap GeneratePatternMap()
    {
        //return new PatternMap("default120");
        return JsonUtility.FromJson<PatternMap>(patternMapJSON.text);
    }

    private void GenerateNotes()
    {
        int nTimes = timeVector.Count;
        Debug.Log(nTimes);
        List<float> timeVectorDifferences = new List<float>(nTimes);
        for(int i=0; i< nTimes - 1; i++)
        {
            timeVectorDifferences.Add(timeVector[i + 1] - timeVector[i]);
        }
        if (musicPlayer.clip.length - timeVector[nTimes - 1] >= 60 / BPM)
        {
            timeVectorDifferences.Add(musicPlayer.clip.length - timeVector[nTimes - 1] + timeVector[0]);
        }
        else
        {
            timeVectorDifferences.Add(timeVectorDifferences[nTimes - 2]);
        }

        //foreach (float f in timeVectorDifferences)
            //Debug.Log(f);

        int nNotes = 2*(int)((barWidth / (timeVectorDifferences.Min()*speed)) + 1);
        //Debug.Log(nNotes);

        noteBufer = new List<GameObject>(nNotes);
        noteScripts = new List<NoteUI>(nNotes);
        noteRectTransforms = new List<RectTransform>(nNotes);

        noteBufferPosition = new Vector2(0, -300);
        //Vector2 hitZonePosition = new Vector2(0, -5);

        
        for (int i = 0; i < nNotes; i++)
        {
            GameObject note = Instantiate(notePrefab, noteBufferPosition, Quaternion.identity);
            note.transform.SetParent(this.transform, false);
            note.transform.SetSiblingIndex(0);

            noteBufer.Add(note);
            NoteUI script = note.GetComponent<NoteUI>();
            noteScripts.Add(script);
            RectTransform noteRectTransform = note.GetComponent<RectTransform>();
            noteRectTransforms.Add(noteRectTransform);
            script.rectTransform = noteRectTransform;
            script.musicPlayer = musicPlayer;
            //script.centerPosition = hitZonePosition;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    bool interLoop = false;
    // Update is called once per frame
    void Update()
    {
        if (hasStarted)
        {
            float musicTime = musicPlayer.time;
            //noteInHitArea = startOKTime <= musicTime && musicTime <= endOKTime;

            if (!noteInHitArea && pulsatingPlayed)
                pulsatingPlayed = false;
            if(noteInHitArea && !pulsatingPlayed)
            {
                hitZone.GetComponent<Animator>().Play("PulsatingKey");
                pulsatingPlayed = true;
            }
            //TODO fix the loop better
            if (!interLoop)
            {
                if (musicTime > firstNoteTime)
                    interLoop = true;
            }
            else
            {
                if (musicTime < timeVector[0])
                    timeVectorIndex = 0;
            }

            //trySetNote();
            if (timeVector[timeVectorIndex] - (barWidth / 2) / speed + patternMap.initialDelay < musicTime)
            {
                //print("timeVector[timeVectorIndex] " + timeVector[timeVectorIndex]);
                //print("(barWidth / 2) / speed " + (barWidth / 2) / speed);
                //print("musicPlayer.time " + musicPlayer.time);
                ///setNextNotes(musicPlayer.time * speed - (timeVector[timeVectorIndex] * speed - (barWidth / 2)));
                setNextNotes();
                timeVectorIndex = (timeVectorIndex + 1) % timeVector.Count;
            }

            /*if (Input.GetKeyDown(KeyCode.P))
                print(musicPlayer.time);

            if (Input.GetKeyDown(KeyCode.L))
                musicPlayer.time = 4.5f * 60;*/
        }
    }

    public void StartNotes()
    {
        if (!hasStarted)
        {
            //print("Hi");
            if (firstNoteTime * speed <= barWidth / 2)
            {
                //print("if");
                ///setNextNotes(barWidth / 2 - firstNoteTime * speed);
                noteRectTransforms[nextNotes].anchoredPosition = new Vector2(-firstNoteTime * speed, -5);
                noteScripts[nextNotes].speed = speed;
                noteScripts[nextNotes].timeToReachCenter = firstNoteTime;
                //noteScripts[nextNotes].firstNote = timeVectorIndex == 0;

                //workingNote = noteBufer[nextNotes + 1];
                noteRectTransforms[nextNotes + 1].anchoredPosition = new Vector2(firstNoteTime * speed, -5);
                noteScripts[nextNotes + 1].speed = -speed;
                noteScripts[nextNotes + 1].timeToReachCenter = firstNoteTime;

                timeVectorIndex = (timeVectorIndex + 1) % timeVector.Count;

                /*startOKTime = firstNoteTime - ((60 / BPM) * hitTime);
                endOKTime = firstNoteTime + ((60 / BPM) * hitTime);
                nextTimeIndex = (nextTimeIndex + 1) % timeVector.Count;*/

                /*if (settingNextNote)
                    return;

                float previousNoteDistance = noteRectTransforms[previousNotes].anchoredPosition.x + (barWidth / 2);
                if (timeVector[timeVectorIndex] * speed <= barWidth / 2)
                {
                    while (previousNoteDistance > timeVector[timeVectorIndex])
                    {
                        ///setNextNotes(previousNoteDistance - timeVector[timeVectorIndex] * speed);
                        setNextNotes();
                        timeVectorIndex = (timeVectorIndex + 1) % timeVector.Count;
                        previousNoteDistance = noteRectTransforms[previousNotes].anchoredPosition.x + (barWidth / 2);
                        //print("p");
                    }
                }
                else
                {
                    StartCoroutine(delayedSetNextNotes(timeVector[timeVectorIndex] - previousNoteDistance / speed));
                }*/
            }
            else
            {
                //print("else");
                StartCoroutine(delayedSetNextNotes(firstNoteTime - (barWidth / 2) / speed));
            }
            //print("Hi2");

            musicPlayer.Play();
            hasStarted = true;
        }
    }

    /*
    public void startNotes()
    {
        if (!hasStarted)
        {
            if (firstNoteDistance <= barWidth / 2)
                setNextNotes(barWidth / 2 - firstNoteDistance);
            else
                StartCoroutine(delayedSetNextNotes((firstNoteDistance - (barWidth / 2)) / speed));

            trySetNote();
            musicPlayer.Play();
            hasStarted = true;
        }
    }

    private void trySetNote()
    {
        if (settingNextNote)
            return;

        float previousNoteDistance = noteRectTransforms[previousNotes].anchoredPosition.x + (barWidth / 2);
        if (distanceVector[distanceVectorIndex] <= barWidth / 2)
        {
            while (previousNoteDistance > distanceVector[distanceVectorIndex])
            {
                setNextNotes(previousNoteDistance - distanceVector[distanceVectorIndex]);
                distanceVectorIndex = (distanceVectorIndex + 1) % distanceVector.Count;
                previousNoteDistance = noteRectTransforms[previousNotes].anchoredPosition.x + (barWidth / 2);
            }
        }
        else
        {
            StartCoroutine(delayedSetNextNotes((distanceVector[distanceVectorIndex] - previousNoteDistance)/speed));
            distanceVectorIndex = (distanceVectorIndex + 1) % distanceVector.Count;
        }
    }
    */

    private bool settingNextNote = false;
    private IEnumerator delayedSetNextNotes(float delay)
    {
        settingNextNote = true;
        yield return new WaitForSeconds(delay);

        ///setNextNotes(0);
        setNextNotes();

        settingNextNote = false;
    }

    /*private void setNextNotes(float offset)
    {
        //GameObject workingNote;

        //workingNote = noteBufer[nextNotes];
        noteRectTransforms[nextNotes].anchoredPosition = new Vector2(-barWidth / 2 + offset, -5);
        noteScripts[nextNotes].speed = speed;
        noteScripts[nextNotes].timeToReachCenter = timeVector[timeVectorIndex];
        //noteScripts[nextNotes].firstNote = timeVectorIndex == 0;

        //workingNote = noteBufer[nextNotes + 1];
        noteRectTransforms[nextNotes + 1].anchoredPosition = new Vector2(barWidth / 2 - offset, -5);
        noteScripts[nextNotes + 1].speed = -speed;
        noteScripts[nextNotes + 1].timeToReachCenter = timeVector[timeVectorIndex];
        //noteScripts[nextNotes + 1].firstNote = timeVectorIndex == 0;

        previousNotes = nextNotes;
        nextNotes = (nextNotes + 2) % noteBufer.Count;
    }*/

    private void setNextNotes()
    {
        //GameObject workingNote;

        //workingNote = noteBufer[nextNotes];
        noteRectTransforms[nextNotes].anchoredPosition = new Vector2(-(timeVector[timeVectorIndex] - musicPlayer.time + patternMap.initialDelay)*speed, -5);
        noteScripts[nextNotes].speed = speed;
        noteScripts[nextNotes].timeToReachCenter = timeVector[timeVectorIndex];
        //noteScripts[nextNotes].firstNote = timeVectorIndex == 0;

        //workingNote = noteBufer[nextNotes + 1];
        noteRectTransforms[nextNotes + 1].anchoredPosition = new Vector2((timeVector[timeVectorIndex] - musicPlayer.time + patternMap.initialDelay) * speed, -5);
        noteScripts[nextNotes + 1].speed = -speed;
        noteScripts[nextNotes + 1].timeToReachCenter = timeVector[timeVectorIndex];
        //noteScripts[nextNotes + 1].firstNote = timeVectorIndex == 0;

        previousNotes = nextNotes;
        nextNotes = (nextNotes + 2) % noteBufer.Count;
    }

    /*public void NextHitTime()
    {
        startOKTime = timeVector[nextTimeIndex] - ((60 / BPM) * hitTime);
        endOKTime = timeVector[nextTimeIndex] + ((60 / BPM) * hitTime);
        nextTimeIndex = (nextTimeIndex + 1) % timeVector.Count;
    }*/

}

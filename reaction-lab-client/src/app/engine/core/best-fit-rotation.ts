import { Quaternion, Vector3 } from "three";

const SQUARINGS = 20;
const CONTINUING_ITERATIONS = 30;

// Below this share of the longest column's squared length, the identity column counts as vanished
const IDENTITY_COLUMN_SHARE = 1e-6;

const CORRELATION = new Float64Array(9);
const HORN_MATRIX = new Float64Array(16);
const SQUARE = new Float64Array(16);
const ITERATED_TURN = new Float64Array(4);
const MULTIPLIED_TURN = new Float64Array(4);

export function bestFitRotation(from: readonly Vector3[], onto: readonly Vector3[], target: Quaternion): Quaternion {
    correlate(from, onto);
    fillShiftedHornMatrix();

    for (let squaring = 0; squaring < SQUARINGS; squaring++) {
        squareHornMatrix();
    }

    return longestColumnOf(target);
}

export function continuedBestFitRotation(from: readonly Vector3[], onto: readonly Vector3[], previous: Quaternion, target: Quaternion): void {
    correlate(from, onto);
    fillShiftedHornMatrix();
    ITERATED_TURN[0] = previous.w;
    ITERATED_TURN[1] = previous.x;
    ITERATED_TURN[2] = previous.y;
    ITERATED_TURN[3] = previous.z;

    for (let iteration = 0; iteration < CONTINUING_ITERATIONS; iteration++) {
        multiplyIteratedTurn();
    }

    target.set(ITERATED_TURN[1], ITERATED_TURN[2], ITERATED_TURN[3], ITERATED_TURN[0]);
}

function correlate(from: readonly Vector3[], onto: readonly Vector3[]): void {
    CORRELATION.fill(0);

    from.forEach((offset, index) => {
        const matching = onto[index];

        CORRELATION[0] += offset.x * matching.x;
        CORRELATION[1] += offset.x * matching.y;
        CORRELATION[2] += offset.x * matching.z;
        CORRELATION[3] += offset.y * matching.x;
        CORRELATION[4] += offset.y * matching.y;
        CORRELATION[5] += offset.y * matching.z;
        CORRELATION[6] += offset.z * matching.x;
        CORRELATION[7] += offset.z * matching.y;
        CORRELATION[8] += offset.z * matching.z;
    });
}

function fillShiftedHornMatrix(): void {
    const xx = CORRELATION[0];
    const xy = CORRELATION[1];
    const xz = CORRELATION[2];
    const yx = CORRELATION[3];
    const yy = CORRELATION[4];
    const yz = CORRELATION[5];
    const zx = CORRELATION[6];
    const zy = CORRELATION[7];
    const zz = CORRELATION[8];

    setSymmetric(0, 0, xx + yy + zz);
    setSymmetric(0, 1, yz - zy);
    setSymmetric(0, 2, zx - xz);
    setSymmetric(0, 3, xy - yx);
    setSymmetric(1, 1, xx - yy - zz);
    setSymmetric(1, 2, xy + yx);
    setSymmetric(1, 3, zx + xz);
    setSymmetric(2, 2, -xx + yy - zz);
    setSymmetric(2, 3, yz + zy);
    setSymmetric(3, 3, -xx - yy + zz);

    let squaredNorm = 0;

    for (let entry = 0; entry < 16; entry++) {
        squaredNorm += HORN_MATRIX[entry] * HORN_MATRIX[entry];
    }

    const norm = Math.sqrt(squaredNorm);

    for (let diagonal = 0; diagonal < 16; diagonal += 5) {
        HORN_MATRIX[diagonal] += norm;
    }
}

function setSymmetric(row: number, column: number, value: number): void {
    HORN_MATRIX[row * 4 + column] = value;
    HORN_MATRIX[column * 4 + row] = value;
}

function squareHornMatrix(): void {
    let largest = 0;

    for (let row = 0; row < 4; row++) {
        for (let column = 0; column < 4; column++) {
            let sum = 0;

            for (let inner = 0; inner < 4; inner++) {
                sum += HORN_MATRIX[row * 4 + inner] * HORN_MATRIX[inner * 4 + column];
            }

            SQUARE[row * 4 + column] = sum;
            largest = Math.max(largest, Math.abs(sum));
        }
    }

    for (let entry = 0; entry < 16; entry++) {
        HORN_MATRIX[entry] = largest > 0 ? SQUARE[entry] / largest : 0;
    }
}

function longestColumnOf(target: Quaternion): Quaternion {
    let longestColumn = 0;
    let longestSquared = -1;

    for (let column = 0; column < 4; column++) {
        let squared = 0;

        for (let row = 0; row < 4; row++) {
            squared += HORN_MATRIX[row * 4 + column] * HORN_MATRIX[row * 4 + column];
        }

        if (squared > longestSquared) {
            longestSquared = squared;
            longestColumn = column;
        }
    }

    let identitySquared = 0;

    for (let row = 0; row < 4; row++) {
        identitySquared += HORN_MATRIX[row * 4] * HORN_MATRIX[row * 4];
    }

    if (identitySquared > IDENTITY_COLUMN_SHARE * longestSquared) {
        longestColumn = 0;
    }

    return target.set(HORN_MATRIX[4 + longestColumn], HORN_MATRIX[8 + longestColumn], HORN_MATRIX[12 + longestColumn], HORN_MATRIX[longestColumn]).normalize();
}

function multiplyIteratedTurn(): void {
    let squaredLength = 0;

    for (let row = 0; row < 4; row++) {
        let sum = 0;

        for (let column = 0; column < 4; column++) {
            sum += HORN_MATRIX[row * 4 + column] * ITERATED_TURN[column];
        }

        MULTIPLIED_TURN[row] = sum;
        squaredLength += sum * sum;
    }

    if (squaredLength === 0) {
        return;
    }

    const length = Math.sqrt(squaredLength);

    for (let row = 0; row < 4; row++) {
        ITERATED_TURN[row] = MULTIPLIED_TURN[row] / length;
    }
}
import type { OffsetSpan } from './types';

export const getSpanLength = (offset: OffsetSpan) => offset.end - offset.start;
